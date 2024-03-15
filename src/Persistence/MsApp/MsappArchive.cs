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
using YamlDotNet.Serialization;

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

    public const string MsappFileExtension = ".msapp";
    public const string YamlFileExtension = ".yaml";
    public const string YamlFxFileExtension = ".pa.yaml";
    public const string JsonFileExtension = ".json";
    public const string AppFileName = $"App{YamlFxFileExtension}";
    public const string HeaderFileName = "Header.json";
    public const string PropertiesFileName = "Properties.json";
    public const string TemplatesFileName = $"{Directories.References}/Templates.json";
    public const string ThemesFileName = $"{Directories.References}/Themes.json";

    #endregion

    #region Fields

    private readonly Lazy<IDictionary<string, ZipArchiveEntry>> _canonicalEntries;
    private App? _app;
    private Header? _header;
    private AppProperties? _appProperties;
    private AppTemplates? _appTemplates;
    private AppThemes? _appThemes;

    private bool _isDisposed;
    private readonly ILogger<MsappArchive>? _logger;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    // Yaml serializer and deserializer
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        WriteIndented = true
    };
    private static readonly JsonWriterOptions JsonWriterOptions = new() { Indented = true };

    #endregion

    #region Internal classes

    /// <summary>
    /// Helper class for deserializing the top level control editor state.
    /// </summary>
    private class TopParentJson
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
        _serializer = yamlSerializationFactory.CreateSerializer();
        _deserializer = yamlSerializationFactory.CreateDeserializer();
        _logger = logger;
        ZipArchive = new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
        CreateGitIgnore();
        _canonicalEntries = new Lazy<IDictionary<string, ZipArchiveEntry>>
        (() =>
        {
            var canonicalEntries = new Dictionary<string, ZipArchiveEntry>();
            // If we're creating a new archive, there are no entries to canonicalize.
            if (mode == ZipArchiveMode.Create)
                return canonicalEntries;
            foreach (var entry in ZipArchive.Entries)
            {
                if (!canonicalEntries.TryAdd(NormalizePath(entry.FullName), entry))
                    _logger?.LogInformation($"Duplicate entry found in archive: {entry.FullName}");
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
            throw new ArgumentNullException("Path cannot be null or whitespace.", nameof(path));
        _ = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var yamlSerializationFactory = serviceProvider.GetRequiredService<IYamlSerializationFactory>();

        return new MsappArchive(path, yamlSerializationFactory);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Canonical entries in the archive.  Keys are normalized paths (lowercase, forward slashes, no trailing slash).
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
            if (_app == null)
                _app = LoadApp();
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
    /// <exception cref="PersistenceException"></exception>
    public T Deserialize<T>(string entryName, bool ensureRoundTrip = true)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentNullException(nameof(entryName));

        var entry = GetRequiredEntry(entryName);
        if (!entry.FullName.EndsWith(YamlFileExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Entry {entryName} is not a yaml file.");

        T result;
        using (var reader = new StreamReader(entry.Open()))
        {
            result = _deserializer.Deserialize<T>(reader);
            if (!ensureRoundTrip)
                return result ?? throw new PersistenceException($"Failed to deserialize archive entry.") { FileName = entry.FullName };
        }

#if DEBUG
        // Expected round trip serialization
        using var stringWriter = new StringWriter();
        _serializer.Serialize(stringWriter, result);
#endif
        // Ensure round trip serialization
        using var roundTripWriter = new RoundTripWriter(new StreamReader(entry.Open()), entryName);
        _serializer.Serialize(roundTripWriter, result);

        return result;
    }

    public T Deserialize<T>(ZipArchiveEntry archiveEntry) where T : class
    {
        _ = archiveEntry ?? throw new ArgumentNullException(nameof(archiveEntry));
        using var textReader = new StreamReader(archiveEntry.Open());
        try
        {
            var result = _deserializer!.Deserialize(textReader) as T;
            return result ?? throw new PersistenceException($"Failed to deserialize archive entry.") { FileName = archiveEntry.FullName };
        }
        catch (Exception ex)
        {
            throw new PersistenceException("Failed to deserialize archive entry.", ex) { FileName = archiveEntry.FullName };
        }
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

            if (directoryName != string.Empty && !entry.Key.StartsWith(directoryName + '/'))
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
    /// <exception cref="FileNotFoundException"></exception>
    public ZipArchiveEntry GetRequiredEntry(string entryName)
    {
        var entry = GetEntry(entryName) ??
            throw new FileNotFoundException($"Entry '{entryName}' not found in msapp archive.");

        return entry;
    }

    /// <inheritdoc/>
    public ZipArchiveEntry CreateEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or whitespace.", nameof(entryName));

        var canonicalEntryName = NormalizePath(entryName);
        if (_canonicalEntries.Value.ContainsKey(canonicalEntryName))
            throw new InvalidOperationException($"Entry {entryName} already exists in the archive.");

        var entry = ZipArchive.CreateEntry(entryName);
        _canonicalEntries.Value.Add(canonicalEntryName, entry);

        return entry;
    }

    /// <inheritdoc/>
    public void Save(Control control, string? directory = null)
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        var controlDirectory = directory == null ? Directories.Src : Path.Combine(Directories.Src, directory);
        var entry = CreateEntry(GetSafeEntryPath(controlDirectory, control.Name, YamlFxFileExtension));

        using (var writer = new StreamWriter(entry.Open()))
        {
            _serializer.Serialize(writer, control);
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

        var appEntry = CreateEntry(Path.Combine(Directories.Src, AppFileName));
        using (var appWriter = new StreamWriter(appEntry.Open()))
        {
            _serializer.Serialize(appWriter, _app);
        }

        foreach (var screen in _app.Screens)
        {
            Save(screen);
        }
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Trim().Replace('\\', '/').TrimStart('/').ToLowerInvariant();
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
        var app = Deserialize<App>(appEntry.FullName);

        app.Screens = LoadScreens();

        return app;
    }

    private List<Screen> LoadScreens()
    {
        _logger?.LogInformation("Loading top level screens from Yaml.");

        var screens = new Dictionary<string, Screen>();
        foreach (var yamlEntry in GetDirectoryEntries(Directories.Src, YamlFileExtension))
        {
            // Skip the app file
            if (yamlEntry.FullName.EndsWith(AppFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            var screen = Deserialize<Screen>(yamlEntry.FullName);
            screens.Add(screen.Name, screen);
        }

        _logger?.LogInformation("Loading top level controls editor state.");
        var controlEditorStates = new Dictionary<string, ControlEditorState>();
        foreach (var editorStateEntry in GetDirectoryEntries(Path.Combine(Directories.Controls), JsonFileExtension))
        {
            try
            {
                var topParentJson = JsonSerializer.Deserialize<TopParentJson>(editorStateEntry.Open());
                controlEditorStates.Add(topParentJson!.TopParent!.Name, topParentJson.TopParent);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to deserialize control editor state file.", ex) { FileName = editorStateEntry.FullName };
            }
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
