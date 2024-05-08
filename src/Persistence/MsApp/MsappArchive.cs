// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Represents a .msapp file.
/// </summary>
public partial class MsappArchive : IMsappArchive, IDisposable
{
    #region Constants

    public static class Directories
    {
        public const string Src = "Src";
        public const string Controls = "Controls";
        public const string Components = "Components";
        public const string AppTests = "AppTests";
        public const string References = "References";
        public const string Resources = "Resources";
    }

    /// <summary>
    /// The separator char that should be used in zip archive entry paths.
    /// </summary>
    public const char EntryPathSeparatorChar = '/';

    public const string MsappFileExtension = ".msapp";
    public const string YamlFileExtension = ".yaml";
    public const string YamlPaFileExtension = ".pa.yaml";
    public const string JsonFileExtension = ".json";
    public const string AppFileName = $"App{YamlPaFileExtension}";
    public const string HeaderFileName = "Header.json";
    public const string PropertiesFileName = "Properties.json";
    public const string TemplatesFileName = $"{Directories.References}/Templates.json";
    public const string ThemesFileName = $"{Directories.References}/Themes.json";
    public const string DataSourcesFileName = $"{Directories.References}/DataSources.json";

    #endregion

    #region Fields

    private readonly Lazy<IDictionary<string, ZipArchiveEntry>> _canonicalEntries;
    private App? _app;
    private Header? _header;
    private AppProperties? _appProperties;
    private AppTemplates? _appTemplates;
    private AppThemes? _appThemes;
    private DataSources? _dataSources;

    private bool _isDisposed;
    private readonly ILogger<MsappArchive>? _logger;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    // Yaml serializer and deserializer
    private readonly IYamlSerializer _yamlSerializer;
    private readonly IYamlDeserializer _yamlDeserializer;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        WriteIndented = true
    };
    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        Indented = true
    };

    #endregion

    #region Internal classes

    /// <summary>
    /// Helper class for deserializing the top level control editor state.
    /// </summary>
    private sealed class TopParentJson
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public ControlEditorState? TopParent { get; set; }
#pragma warning restore CS0649
    }

    #endregion

    #region Constructors

    public MsappArchive(string path, IYamlSerializationFactory yamlSerializationFactory, ILogger<MsappArchive>? logger = null)
        : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, leaveOpen: false, yamlSerializationFactory, logger)
    {
    }

    public MsappArchive(Stream stream, IYamlSerializationFactory yamlSerializationFactory, ILogger<MsappArchive>? logger = null)
        : this(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null, yamlSerializationFactory, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, IYamlSerializationFactory yamlSerializationFactory, ILogger<MsappArchive>? logger = null)
        : this(stream, mode, leaveOpen: false, entryNameEncoding: null, yamlSerializationFactory, logger)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="mode"></param>
    /// <param name="leaveOpen">
    ///     true to leave the stream open after the System.IO.Compression.ZipArchive object is disposed; otherwise, false
    /// </param>
    /// <param name="yamlSerializationFactory"></param>
    /// <param name="logger"></param>
    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, IYamlSerializationFactory yamlSerializationFactory, ILogger<MsappArchive>? logger = null)
        : this(stream, mode, leaveOpen, null, yamlSerializationFactory, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding, IYamlSerializationFactory yamlSerializationFactory, ILogger<MsappArchive>? logger = null)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _yamlSerializer = yamlSerializationFactory.CreateSerializer();
        _yamlDeserializer = yamlSerializationFactory.CreateDeserializer();
        _logger = logger;
        ZipArchive = new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
        CreateGitIgnore();
        _canonicalEntries = new Lazy<IDictionary<string, ZipArchiveEntry>>
        (() =>
        {
            var canonicalEntries = new Dictionary<string, ZipArchiveEntry>(StringComparer.InvariantCultureIgnoreCase);

            // If we're creating a new archive, there are no entries to canonicalize.
            if (mode != ZipArchiveMode.Create)
            {
                foreach (var entry in ZipArchive.Entries)
                {
                    if (!canonicalEntries.TryAdd(NormalizeEntryPathUsingCanonicalStrategy(entry.FullName), entry))
                        _logger?.LogInformation("Duplicate entry found in archive: {EntryFullName}", entry.FullName);
                }
            }

            return canonicalEntries;
        });
    }

    #endregion

    #region Factory Methods

    public static IMsappArchive Create(string path, IYamlSerializationFactory yamlSerializationFactory, ILogger<MsappArchive>? logger = null)
    {
        var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return new MsappArchive(fileStream, ZipArchiveMode.Create, yamlSerializationFactory, logger);
    }

    public static IMsappArchive Open(string path, IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path), "Path cannot be null or whitespace.");
        _ = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var yamlSerializationFactory = serviceProvider.GetRequiredService<IYamlSerializationFactory>();

        return new MsappArchive(path, yamlSerializationFactory);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Canonical entries in the archive.  Keys are normalized paths (lowercase, forward slashes, no trailing slash) using <see cref="NormalizeEntryPathUsingCanonicalStrategy(string)"/>.
    /// </summary>
    public IReadOnlyDictionary<string, ZipArchiveEntry> CanonicalEntries => _canonicalEntries.Value.AsReadOnly();

    /// <inheritdoc/>
    public ZipArchive ZipArchive { get; private set; }

    /// <summary>
    /// Total sum of decompressed sizes of all entries in the archive.
    /// </summary>
    public long DecompressedSize => ZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.Length);

    /// <summary>
    /// Total sum of compressed sizes of all entries in the archive.
    /// </summary>
    public long CompressedSize => ZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.CompressedLength);

    public App? App
    {
        get
        {
            _app ??= LoadApp();
            return _app;
        }
        set
        {
            _app = value;
            _header = _app != null ? new Header() : null;
            _appProperties = _app != null ? new AppProperties() : null;
            _appTemplates = _app != null ? new AppTemplates() : null;
            _appThemes = _app != null ? new AppThemes() : null;
        }
    }

    public Version Version
    {
        get
        {
            _header ??= LoadHeader();

            return _header.MSAppStructureVersion;
        }
    }

    public Version DocVersion
    {
        get
        {
            _header ??= LoadHeader();

            return _header.DocVersion;
        }
    }
    public AppProperties? Properties
    {
        get
        {
            _appProperties ??= LoadProperties();

            return _appProperties;
        }
        set => _appProperties = value;
    }

    public DataSources? DataSources
    {
        get
        {
            _dataSources ??= LoadDataSources();

            return _dataSources;
        }

        set => _dataSources = value;
    }

    public bool AddGitIgnore { get; init; } = true;

    #endregion

    #region Methods

    /// <summary>
    /// Deserializes the entry with the given name into an object of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entryName"></param>
    /// <param name="ensureRoundTrip"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="PaPersistenceException"></exception>
    public T Deserialize<T>(string entryName, bool ensureRoundTrip = true) where T : Control
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentNullException(nameof(entryName));

        var entry = GetRequiredEntry(entryName);
        var result = Deserialize<T>(entry);

        if (ensureRoundTrip)
        {
#if DEBUG
            // Expected round trip serialization
            using var stringWriter = new StringWriter();
            _yamlSerializer.SerializeControl(stringWriter, result);
#endif
            // Ensure round trip serialization
            using var roundTripWriter = new RoundTripWriter(entry);
            _yamlSerializer.SerializeControl(roundTripWriter, result);
        }

        return result;
    }

    public T Deserialize<T>(ZipArchiveEntry archiveEntry) where T : Control
    {
        _ = archiveEntry ?? throw new ArgumentNullException(nameof(archiveEntry));

        if (!archiveEntry.FullName.EndsWith(YamlFileExtension, StringComparison.OrdinalIgnoreCase))
            throw new PaPersistenceException(PersistenceErrorCode.MsappArchiveError, $"Entry {archiveEntry} is not a yaml file.") { MsappEntryFullPath = archiveEntry.FullName };

        using var textReader = new StreamReader(archiveEntry.Open());
        return _yamlDeserializer.Deserialize<T>(textReader)
            ?? throw new PaPersistenceException(PersistenceErrorCode.EditorStateJsonEmptyOrNull, "Deserialization of file resulted in null object.") { MsappEntryFullPath = archiveEntry.FullName };
    }

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null, bool recursive = true)
    {
        directoryName = NormalizePath(directoryName).TrimEnd('/');

        foreach (var entry in CanonicalEntries)
        {
            // Do not return directories which some zip implementations include as entries
            if (entry.Key.EndsWith('/'))
                continue;

            if (directoryName != string.Empty && !entry.Key.StartsWith(directoryName + '/', StringComparison.InvariantCulture))
                continue;

            // If not recursive, skip subdirectories
            if (!recursive && entry.Key.IndexOf('/', directoryName.Length == 0 ? 0 : directoryName.Length + 1) > 0)
                continue;

            if (extension != null && !entry.Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return entry.Value;
        }
    }

    /// <inheritdoc/>
    public ZipArchiveEntry? GetEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            return null;

        entryName = NormalizePath(entryName);
        if (CanonicalEntries.TryGetValue(entryName, out var entry))
            return entry;

        return null;
    }

    /// <summary>
    /// Returns the entry in the archive with the given name or throws if it does not exist.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="PaPersistenceException"></exception>
    public ZipArchiveEntry GetRequiredEntry(string entryName)
    {
        return GetEntry(entryName) ??
            throw new PaPersistenceException(PersistenceErrorCode.MsappArchiveError, $"Entry with name '{entryName}' not found in msapp archive.");
    }

    /// <inheritdoc/>
    public ZipArchiveEntry CreateEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentNullException(nameof(entryName));

        var canonicalEntryName = NormalizePath(entryName);
        if (_canonicalEntries.Value.ContainsKey(canonicalEntryName))
            throw new InvalidOperationException($"Entry {entryName} already exists in the archive.");

        var entry = ZipArchive.CreateEntry(entryName);
        _canonicalEntries.Value.Add(canonicalEntryName, entry);

        return entry;
    }

    /// <summary>
    /// Creates a new entry with a unique file name based on the <paramref name="nameNoExt"/>.
    /// If an entry already exists in the target directory with the same effective fileName, a number will be appended to the filename in such a way
    /// to make the new entry unique.
    /// </summary>
    /// <param name="directoryPath">
    /// The optional directory path within which the new entry should be created. May be null/empty.
    /// This path will get normalized to use the correct directory separators for entries (i.e. '/').
    /// </param>
    /// <param name="nameNoExt">
    /// The desired file name for the entry but with no extenaion. Specify the extension via <paramref name="extension"/>.
    /// </param>
    /// <param name="extension">The file extension to append to the file name for the entry. May be null or empty when the file shouldn't have an extension.</param>
    /// <returns>A new <see cref="ZipArchiveEntry"/> that has a file name which is guaranteed to be unique in the same folder.</returns>
    public ZipArchiveEntry CreateUniqueEntry(string? directoryPath, string nameNoExt, string? extension)
    {
        directoryPath ??= string.Empty;
        ArgumentException.ThrowIfNullOrEmpty(nameNoExt, nameof(nameNoExt));
        extension ??= string.Empty;

        var fullPathNoExt = string.IsNullOrEmpty(directoryPath) ? nameNoExt : Path.Combine(directoryPath, nameNoExt);
        fullPathNoExt = NormalizePath(fullPathNoExt);

        var fullPath = $"{fullPathNoExt}{extension}";
        if (!CanonicalEntries.ContainsKey(NormalizePath(fullPathNoExt)))
    }

    /// <summary>
    /// Replaces all unsafe zip entry filename characters with the specified <paramref name="replacement"/>.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="replacement">The replacement. Default is an empty string.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is null or empty.</exception>
    /// <exception cref="ArgumentException"
    public static string ReplaceUnsafeFileNameChars(string fileName, string? replacement = null)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        var safeName = SafeFileNameRegex().Replace(fileName, replacement ?? string.Empty);

        if (string.IsNullOrWhiteSpace(safeName))
            throw new ArgumentException("The resulting safe filename is empty or all whitespace, which is an invalid filename entry. Consider specifying a replacement which is not the empty string.");

        return safeName;
    }

    /// <inheritdoc/>
    public void Save(Control control, string? directory = null)
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        var controlDirectory = directory == null ? Directories.Src : Path.Combine(Directories.Src, directory);
        var entry = CreateEntry(GetSafeEntryPath(controlDirectory, control.Name, YamlPaFileExtension));

        using (var writer = new StreamWriter(entry.Open()))
        {
            _yamlSerializer.SerializeControl(writer, control);
        }

        SaveEditorState(control);
    }

    private string GetSafeEntryPath(string directory, string name, string extension)
    {
        var safeName = SafeFileNameRegex().Replace(name, "").Trim();
        if (string.IsNullOrWhiteSpace(safeName))
            throw new ArgumentException("Control name is not valid.", nameof(name));

        var entryPath = Path.Combine(directory, $"{safeName}{extension}");
        if (!CanonicalEntries.ContainsKey(NormalizePath(entryPath)))
            return entryPath;

        // If file with the same name already exists, add a number to the end of the name
        for (var i = 1; i < int.MaxValue; i++)
        {
            var nextSafeName = $"{safeName}{i}";
            entryPath = Path.Combine(directory, $"{nextSafeName}{extension}");
            if (!CanonicalEntries.ContainsKey(NormalizePath(entryPath)))
                return entryPath;
        }

        throw new InvalidOperationException("Failed to find a unique name for the control.");
    }

    public void Save()
    {
        if (_app == null || _header == null)
            throw new InvalidOperationException("App or header are not set.");

        SaveHeader();
        SaveProperties();
        SaveTemplates();
        SaveThemes();
        SaveDataSources();

        var appEntry = CreateEntry(Path.Combine(Directories.Src, AppFileName));
        using (var appWriter = new StreamWriter(appEntry.Open()))
        {
            _yamlSerializer.SerializeControl(appWriter, _app);
        }

        foreach (var screen in _app.Screens)
        {
            Save(screen);
        }
    }

    // BUG: This function should be internal and only accessabile via tests.
    /// <summary>
    /// DO NOT use this function to fix entry paths for entry into the archive. It should only be used internally to test and detect duplicate entries.<br/>
    /// Normalizes an entry path to an invariant key that is used to identify duplicate entries.
    /// It does this by first trimming, replaces '\' chars with '/', and removes all '/' chars at the start and then converts to the LowerInvariant.
    /// </summary>
    public static string NormalizeEntryPathUsingCanonicalStrategy(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return NormalizeEntryPath(path).ToLowerInvariant();
    }

    // BUG: This should be internal.
    /// <summary>
    /// Normalizes an entry path to ensure Trims the path, replaces '\' chars with '/', and removes all '/' chars at the start and converts to the LowerInvariant.
    /// </summary>
    public static string NormalizeEntryPath(string path)
    {
        _ = path ?? throw new ArgumentNullException(path);

        var normalized = path.Trim().Replace('\\', EntryPathSeparatorChar).TrimStart(EntryPathSeparatorChar);
    }

    [GeneratedRegex("[^a-zA-Z0-9_\\- ]")]
    private static partial Regex SafeFileNameRegex();

    #endregion

    #region Private Methods

    private App? LoadApp()
    {
        // For app entry name is always "App.pa.yaml" now
        var appEntry = GetEntry(Path.Combine(Directories.Src, AppFileName));
        if (appEntry == null)
            return null;

        var app = Deserialize<App>(appEntry.FullName, ensureRoundTrip: false);

        app.Screens = LoadScreens();

        return app;
    }

    private List<Screen> LoadScreens()
    {
        _logger?.LogInformation("Loading top level screens from Yaml.");

        var screens = new Dictionary<string, Screen>();
        foreach (var yamlEntry in GetDirectoryEntries(Directories.Src, YamlFileExtension, recursive: false))
        {
            // Skip the app file
            if (yamlEntry.FullName.EndsWith(AppFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            var screen = Deserialize<Screen>(yamlEntry.FullName, ensureRoundTrip: false);
            screens.Add(screen.Name, screen);
        }

        _logger?.LogInformation("Loading top level controls editor state.");
        var controlEditorStates = new Dictionary<string, ControlEditorState>();
        foreach (var editorStateEntry in GetDirectoryEntries(Path.Combine(Directories.Controls), JsonFileExtension))
        {
            var topParentJson = DeserializeMsappJsonFile<TopParentJson>(editorStateEntry);
            controlEditorStates.Add(topParentJson!.TopParent!.Name, topParentJson.TopParent);
        }

        // Merge the editor state into the controls
        foreach (var control in screens.Values)
        {
            if (controlEditorStates.TryGetValue(control.Name, out var editorState))
            {
                MergeControlEditorState(control, editorState);
                controlEditorStates.Remove(control.Name);
            }
        }

        return screens.Values.ToList();
    }

    private static void MergeControlEditorState(Control control, ControlEditorState controlEditorState)
    {
        control.EditorState = controlEditorState;
        if (control.Children == null)
            return;

        foreach (var child in control.Children)
        {
            if (controlEditorState.Controls == null)
                continue;

            // Find the editor state for the child by name
            var childEditorState = controlEditorState.Controls.FirstOrDefault(c => c.Name == child.Name);
            if (childEditorState == null)
                continue;

            MergeControlEditorState(child, childEditorState);
        }
        controlEditorState.Controls = null;
    }

    private void SaveHeader()
    {
        var entry = CreateEntry(HeaderFileName);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, _header, JsonSerializerOptions);
    }

    private Header LoadHeader()
    {
        var entry = GetRequiredEntry(HeaderFileName);
        var header = DeserializeMsappJsonFile<Header>(entry);
        return header;
    }

    private AppProperties? LoadProperties()
    {
        var entry = GetEntry(PropertiesFileName);
        if (entry == null)
            return null;

        var appProperties = DeserializeMsappJsonFile<AppProperties>(entry);
        return appProperties;
    }

    private DataSources? LoadDataSources()
    {
        var entry = GetEntry(DataSourcesFileName);
        if (entry == null)
            return null;

        var dataSources = DeserializeMsappJsonFile<DataSources>(entry);
        return dataSources;
    }

    private void SaveProperties()
    {
        var entry = CreateEntry(PropertiesFileName);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, _appProperties, JsonSerializerOptions);
    }

    private void SaveTemplates()
    {
        var entry = CreateEntry(TemplatesFileName);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, _appTemplates, JsonSerializerOptions);
    }

    private void SaveThemes()
    {
        var entry = CreateEntry(ThemesFileName);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, _appThemes, JsonSerializerOptions);
    }

    private void SaveDataSources()
    {
        if (_dataSources == null || _dataSources.Items == null || _dataSources.Items.Count == 0)
            return;

        var entry = CreateEntry(DataSourcesFileName);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, _dataSources, JsonSerializerOptions);
    }

    private void SaveEditorState(Control control)
    {
        if (control.EditorState == null)
            return;
        var entry = CreateEntry(GetSafeEntryPath(Directories.Controls, control.Name, JsonFileExtension));
        var topParent = new TopParentJson
        {
            TopParent = MapEditorState(control)
        };

        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, topParent, JsonSerializerOptions);
    }

    private static ControlEditorState MapEditorState(Control control)
    {
        var editorState = control.EditorState ?? new ControlEditorState(control);
        if (control.Children == null || control.Children.Count == 0)
            return editorState;

        editorState.Controls = control.Children.Select(MapEditorState).ToList();
        return editorState;
    }

    private void CreateGitIgnore()
    {
        if (!AddGitIgnore || ZipArchive.Mode != ZipArchiveMode.Create)
            return;

        var entry = ZipArchive.CreateEntry(".gitignore");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream);
        writer.WriteLine("## MsApp specific overrides");
        writer.WriteLine("/[Cc]ontrols/");
        writer.WriteLine("/[Cc]hecksum.json");
        writer.WriteLine("/[Hh]eader.json");
        writer.WriteLine("/[Aa]pp[Cc]hecker[Rr]esult.sarif");
    }

    private static T DeserializeMsappJsonFile<T>(ZipArchiveEntry entry)
        where T : notnull
    {
        try
        {
            return JsonSerializer.Deserialize<T>(entry.Open())
                 ?? throw new PaPersistenceException(PersistenceErrorCode.EditorStateJsonEmptyOrNull, "Deserialization of json file resulted in null object.") { MsappEntryFullPath = entry.FullName };
        }
        catch (JsonException ex)
        {
            throw new PaPersistenceException(PersistenceErrorCode.InvalidEditorStateJson, $"Failed to deserialize json file to an instance of {typeof(T).Name}.", ex)
            {
                MsappEntryFullPath = entry.FullName,
                LineNumber = ex.LineNumber,
                Column = ex.BytePositionInLine,
                JsonPath = ex.Path,
            };
        }
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                ZipArchive.Dispose();
                if (!_leaveOpen)
                {
                    _stream.Dispose();
                }
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
