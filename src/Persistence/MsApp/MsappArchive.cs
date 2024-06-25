// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
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
        public const string Assets = "Assets";
        public const string Images = "Images";
        public const string References = "References";
        public const string Resources = "Resources";
    }

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
    public const string ResourcesFileName = $"{Directories.References}/Resources.json";

    #endregion

    #region Fields

    private readonly Lazy<IDictionary<string, ZipArchiveEntry>> _canonicalEntries;
    private App? _app;
    private Header? _header;
    private AppProperties? _appProperties;
    private AppTemplates? _appTemplates;
    private AppThemes? _appThemes;
    private DataSources? _dataSources;
    private Resources? _resources;

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
            var canonicalEntries = new Dictionary<string, ZipArchiveEntry>();
            // If we're creating a new archive, there are no entries to canonicalize.
            if (mode == ZipArchiveMode.Create)
                return canonicalEntries;
            foreach (var entry in ZipArchive.Entries)
            {
                if (!canonicalEntries.TryAdd(CanonicalizePath(entry.FullName), entry))
                    _logger?.DuplicateEntry(entry.FullName);
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

    /// <inheritdoc/>
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

    public Resources? Resources
    {
        get
        {
            _resources ??= LoadResources();

            return _resources;
        }

        set => _resources = value;
    }

    public bool AddGitIgnore { get; init; } = true;

    #endregion

    #region Methods

    /// <inheritdoc/>
    public bool DoesEntryExist(string entryPath)
    {
        _ = entryPath ?? throw new ArgumentNullException(nameof(entryPath));

        return CanonicalEntries.ContainsKey(CanonicalizePath(entryPath));
    }

    /// <inheritdoc/>
    public string AddImage(string fileName, Stream imageStream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        ArgumentNullException.ThrowIfNull(imageStream);

        var imagePath = Path.Combine(Directories.Assets, Directories.Images, Path.GetFileName(fileName));
        if (TryGetEntry(imagePath, out _))
            throw new InvalidOperationException($"Image {fileName} already exists in the archive.");

        var imageEntry = CreateEntry(imagePath);
        using (var entryStream = imageEntry.Open())
        {
            imageStream.CopyTo(entryStream);
        }

        // Register the image as a resource
        var resourceName = Path.GetFileNameWithoutExtension(fileName);
        _resources ??= new Resources();
        _resources.Items.Add(new Resource
        {
            Name = resourceName,
            Schema = "i",
            FileName = fileName,
            Path = imagePath,
            Content = "Image",
        });

        return resourceName;
    }

    /// <summary>
    /// Deserializes the entry with the given name into an object of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entryName"></param>
    /// <param name="ensureRoundTrip"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="PersistenceLibraryException"></exception>
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
            throw new PersistenceLibraryException(PersistenceErrorCode.MsappArchiveError, $"Entry {archiveEntry} is not a yaml file.") { MsappEntryFullPath = archiveEntry.FullName };

        using var textReader = new StreamReader(archiveEntry.Open());
        return _yamlDeserializer.Deserialize<T>(textReader)
            ?? throw new PersistenceLibraryException(PersistenceErrorCode.EditorStateJsonEmptyOrNull, "Deserialization of file resulted in null object.") { MsappEntryFullPath = archiveEntry.FullName };
    }

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null, bool recursive = true)
    {
        directoryName = CanonicalizePath(directoryName).TrimEnd('/');

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
        return TryGetEntry(entryName, out var entry) ? entry : null;
    }

    /// <inheritdoc/>
    public bool TryGetEntry(string entryName, [MaybeNullWhen(false)] out ZipArchiveEntry zipArchiveEntry)
    {
        _ = entryName ?? throw new ArgumentNullException(nameof(entryName));

        if (string.IsNullOrWhiteSpace(entryName))
        {
            zipArchiveEntry = null;
            return false;
        }

        return CanonicalEntries.TryGetValue(CanonicalizePath(entryName), out zipArchiveEntry);
    }

    /// <inheritdoc/>
    public ZipArchiveEntry GetRequiredEntry(string entryName)
    {
        return TryGetEntry(entryName, out var entry)
            ? entry
            : throw new PersistenceLibraryException(PersistenceErrorCode.MsappArchiveError, $"Entry with name '{entryName}' not found in msapp archive.");
    }

    /// <inheritdoc/>
    public ZipArchiveEntry CreateEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentNullException(nameof(entryName));

        var canonicalEntryName = CanonicalizePath(entryName);
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
        var entry = CreateEntry(GetSafeEntryPath(controlDirectory, control.Name, YamlPaFileExtension));

        using (var writer = new StreamWriter(entry.Open()))
        {
            _yamlSerializer.SerializeControl(writer, control);
        }

        SaveEditorState(control);
    }

    private string GetSafeEntryPath(string directory, string name, string extension)
    {
        if (!TryMakeSafeForEntryPathSegment(name, out var safeName, unsafeCharReplacementText: ""))
            throw new ArgumentException("Control name is not valid.", nameof(name));

        return GenerateUniqueEntryPath(directory.WhiteSpaceToNull(), safeName, extension);
    }

    /// <inheritdoc/>
    public string GenerateUniqueEntryPath(
        string? directory,
        string fileNameNoExtension,
        string? extension,
        string uniqueSuffixSeparator = "")
    {
        if (directory != null && string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The directory can be null, but cannot be empty or whitespace only.", nameof(directory));
        }
        _ = fileNameNoExtension ?? throw new ArgumentNullException(nameof(fileNameNoExtension));
        if (!IsSafeForEntryPathSegment(fileNameNoExtension))
        {
            throw new ArgumentException($"The {nameof(fileNameNoExtension)} must be safe for use as an entry path segment. Prevalidate using {nameof(TryMakeSafeForEntryPathSegment)} first.", nameof(fileNameNoExtension));
        }
        if (extension != null && !IsSafeForEntryPathSegment(extension))
        {
            throw new ArgumentException("The extension can be null, but cannot be empty or whitespace only, and must be a valid entry path segment.", nameof(directory));
        }

        var entryPathPrefix = $"{NormalizeDirectoryEntryPath(directory)}{fileNameNoExtension}";

        // First see if we can use the name as is
        var entryPath = $"{entryPathPrefix}{extension}";
        if (!DoesEntryExist(entryPath))
            return entryPath;

        // If file with the same name already exists, add a number to the end of the name
        entryPathPrefix += uniqueSuffixSeparator;
        for (var i = 1; i < int.MaxValue; i++)
        {
            entryPath = $"{entryPathPrefix}{i}{extension}";
            if (!DoesEntryExist(entryPath))
                return entryPath;
        }

        throw new InvalidOperationException("Failed to generate a unique name.");
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
        SaveResources();

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

    /// <summary>
    /// Canonicalizes an entry path to a value used in the canonical entries dictionary (<see cref="IMsappArchive.CanonicalEntries"/>).
    /// It removes leading and trailing slashes, converts backslashes to forward slashes, and makes the path lowercase.
    /// </summary>
    public static string CanonicalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Trim().Replace('\\', '/').TrimStart('/').ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a directory path so it can be used as a prefix for entry paths.<br/>
    /// Normalized directory paths are different than file entry paths, in that they end with the platform separator character.<br/>
    /// Directory paths end with '/' unless they are at the root.
    /// </summary>
    /// <returns>Empty string for root directory or a path that ends with '/' for a sub-directory.</returns>
    public static string NormalizeDirectoryEntryPath(string? directoryPath)
    {
        // we assume that each segment of the path is already valid (i.e. doesn't have leading/trailing whitespace)
        // Callers should ensure they enter valid path segments. We can add additional validation if this becomes a problem.

        if (string.IsNullOrEmpty(directoryPath))
            return string.Empty;

        var normalizedPath = EntryPathDirectorySeparatorsRegex()
            .Replace(directoryPath, Path.DirectorySeparatorChar.ToString())
            .Trim(Path.DirectorySeparatorChar); // we trim the ends of all '/' chars so that we can be sure to add only a single one at the end

        if (normalizedPath.Length == 0)
            return string.Empty;

        return normalizedPath + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Makes a user-provided name safe for use as an entry path segment in the archive.
    /// After making the name safe, it will be trimmed and empty strings will result in a false return value.
    /// </summary>
    /// <param name="unsafeName">An unsafe name which may contain invalid chars for usage in an entry path segment (e.g. directory name or file name).</param>
    /// <param name="unsafeCharReplacementText">Unsafe characters in the name will be replaced with this string. Default is empty string.</param>
    /// <returns>true, when <paramref name="unsafeName"/> was converted to a safe, non-empty string; otherwise, false indicates that input could not be turned into a safe, non-empty string.</returns>
    public static bool TryMakeSafeForEntryPathSegment(
        string unsafeName,
        [NotNullWhen(true)]
        out string? safeName,
        string unsafeCharReplacementText = "")
    {
        _ = unsafeName ?? throw new ArgumentNullException(nameof(unsafeName));
        _ = unsafeCharReplacementText ?? throw new ArgumentNullException(nameof(unsafeCharReplacementText));

        safeName = UnsafeFileNameCharactersRegex()
            .Replace(unsafeName, unsafeCharReplacementText)
            .Trim()
            .EmptyToNull();

        return safeName != null;
    }

    /// <summary>
    /// Used to verify that a name is safe for use as a single path segment for an entry.
    /// Directory separator chars are not allowed in a path segment.
    /// </summary>
    /// <param name="name">The proposed path segment name.</param>
    /// <returns>false when <paramref name="name"/> is null, empty, whitespace only, has leading or trailing whitespace, contains path separator chars or contains any other invalid chars.</returns>
    public static bool IsSafeForEntryPathSegment(string name)
    {
        _ = name ?? throw new ArgumentNullException(nameof(name));

        return !string.IsNullOrWhiteSpace(name)
            && !UnsafeFileNameCharactersRegex().IsMatch(name)
            && name.Trim() == name; // No leading or trailing whitespace
    }

    /// <summary>
    /// Regular expression that matches any characters that are unsafe for entry filenames.<br/>
    /// Note: we don't allow any sort of directory separator chars for filenames to remove cross-platform issues.
    /// </summary>
    [GeneratedRegex("[^a-zA-Z0-9 ._-]")]
    private static partial Regex UnsafeFileNameCharactersRegex();

    /// <summary>
    /// Matches the directory separators in an entry path.
    /// </summary>
    [GeneratedRegex(@"[/\\]+")]
    private static partial Regex EntryPathDirectorySeparatorsRegex();

    #endregion

    #region Private Methods

    private App? LoadApp()
    {
        // For app entry name is always "App.pa.yaml" now
        if (!TryGetEntry(Path.Combine(Directories.Src, AppFileName), out var appEntry))
            return null;

        var app = Deserialize<App>(appEntry.FullName, ensureRoundTrip: false);

        app.Screens = LoadScreens();

        return app;
    }

    private List<Screen> LoadScreens()
    {
        _logger?.InfoMessage("Loading top level screens from Yaml.");

        var screens = new Dictionary<string, Screen>();
        foreach (var yamlEntry in GetDirectoryEntries(Directories.Src, YamlFileExtension, recursive: false))
        {
            // Skip the app file
            if (yamlEntry.FullName.EndsWith(AppFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            var screen = Deserialize<Screen>(yamlEntry.FullName, ensureRoundTrip: false);
            screens.Add(screen.Name, screen);
        }

        _logger?.InfoMessage("Loading top level controls editor state.");
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
        if (!TryGetEntry(PropertiesFileName, out var entry))
            return null;

        var appProperties = DeserializeMsappJsonFile<AppProperties>(entry);
        return appProperties;
    }

    private DataSources? LoadDataSources()
    {
        if (!TryGetEntry(DataSourcesFileName, out var entry))
            return null;

        var dataSources = DeserializeMsappJsonFile<DataSources>(entry);
        return dataSources;
    }

    private Resources? LoadResources()
    {
        if (!TryGetEntry(ResourcesFileName, out var entry))
            return null;

        var resources = DeserializeMsappJsonFile<Resources>(entry);
        return resources;
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

    private void SaveResources()
    {
        if (_resources == null || _resources.Items == null || _resources.Items.Count == 0)
            return;

        var entry = CreateEntry(ResourcesFileName);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream, JsonWriterOptions);
        JsonSerializer.Serialize(writer, _resources, JsonSerializerOptions);
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
                 ?? throw new PersistenceLibraryException(PersistenceErrorCode.EditorStateJsonEmptyOrNull, "Deserialization of json file resulted in null object.") { MsappEntryFullPath = entry.FullName };
        }
        catch (JsonException ex)
        {
            throw new PersistenceLibraryException(PersistenceErrorCode.InvalidEditorStateJson, $"Failed to deserialize json file to an instance of {typeof(T).Name}.", ex)
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
