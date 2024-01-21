// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.Formulas.Tools.MsApp;

/// <summary>
/// Represents a .msapp file.
/// </summary>
public class MsappArchive : IMsappArchive, IDisposable
{
    #region Constants

    public const string SrcDirectory = "Src";
    public const string ControlsDirectory = "Controls";
    public const string ComponentsDirectory = "Components";
    public const string AppTestDirectory = "AppTests";
    public const string ReferencesDirectory = "References";
    public const string ResourcesDirectory = "Resources";

    public const string YamlFileExtension = ".yaml";
    public const string YamlFxFileExtension = ".fx.yaml";
    public const string JsonFileExtension = ".json";

    #endregion

    #region Fields

    private Lazy<IDictionary<string, ZipArchiveEntry>> _canonicalEntries;
    private Lazy<List<Control>> _topLevelControls;
    private bool _isDisposed;
    private readonly ILogger<MsappArchive> _logger;
    private FileStream _fileStream;
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

    #endregion

    #region Internal classes

    /// <summary>
    /// Helper class for deserializing the top level control editor state.
    /// </summary>
    private class TopParentJson
    {
        public ControlEditorState TopParent { get; set; }
    }

    #endregion

    #region Constructors

    public MsappArchive(string fileName, ILogger<MsappArchive> logger = null)
    {
        _logger = logger;
        _fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        Initialize(_fileStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

    }

    public MsappArchive(Stream stream, ILogger<MsappArchive> logger = null)
        : this(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, ILogger<MsappArchive> logger = null)
        : this(stream, mode, leaveOpen: false, entryNameEncoding: null, logger)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="mode"></param>
    /// <param name="leaveOpen">true to leave the stream open after the System.IO.Compression.ZipArchive object is disposed; otherwise, false</param>
    /// <param name="logger"></param>
    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, ILogger<MsappArchive> logger = null)
        : this(stream, mode, leaveOpen, null, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding entryNameEncoding, ILogger<MsappArchive> logger = null)
    {
        _logger = logger;
        Initialize(stream, mode, leaveOpen, entryNameEncoding);
    }

    private void Initialize(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding entryNameEncoding)
    {
        ZipArchive = new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
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
        _topLevelControls = new Lazy<List<Control>>(LoadTopLevelControls);
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

    public IReadOnlyList<Control> TopLevelControls => _topLevelControls.Value.AsReadOnly();

    #endregion

    #region Methods

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    /// <param name="directoryName"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    public IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string extension = null)
    {
        _ = directoryName ?? throw new ArgumentNullException(nameof(directoryName));

        directoryName = NormalizePath(directoryName);

        foreach (var entry in CanonicalEntries)
        {
            if (!entry.Key.StartsWith(directoryName + '/'))
                continue;

            if (extension != null && !entry.Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return entry.Value;
        }
    }

    /// <inheritdoc/>
    public ZipArchiveEntry GetEntry(string entryName)
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

    public static string NormalizePath(string path)
    {
        return path.Trim().Replace('\\', '/').Trim('/').ToLowerInvariant();
    }

    #endregion

    #region Private Methods

    private List<Control> LoadTopLevelControls()
    {
        _logger?.LogInformation("Loading top level controls from Yaml.");

        var controls = new Dictionary<string, Control>();
        foreach (var yamlEntry in GetDirectoryEntries(Path.Combine(SrcDirectory, ControlsDirectory), YamlFileExtension))
        {
            using var textReader = new StreamReader(yamlEntry.Open());
            try
            {
                var control = YamlDeserializer.Deserialize<Control>(textReader);
                controls.Add(control.Name, control);
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to deserialize control yaml file.", yamlEntry.FullName, ex);
            }
        }

        _logger?.LogInformation("Loading top level controls editor state.");
        var controlEditorStates = new Dictionary<string, ControlEditorState>();
        foreach (var editorStateEntry in GetDirectoryEntries(Path.Combine(ControlsDirectory), JsonFileExtension))
        {
            try
            {
                var topParentJson = JsonSerializer.Deserialize<TopParentJson>(editorStateEntry.Open());
                controlEditorStates.Add(topParentJson.TopParent.Name, topParentJson.TopParent);
            }
            catch (Exception ex)
            {
                throw new SerializationException("Failed to deserialize control editor state file.", editorStateEntry.FullName, ex);
            }
        }

        // Merge the editor state into the controls
        foreach (var control in controls.Values)
        {
            if (controlEditorStates.TryGetValue(control.Name, out var editorState))
            {
                MergeControlEditorState(control, editorState);
                controlEditorStates.Remove(control.Name);
            }
        }

        // For backwards compatibility, add any editor states that don't have a matching control
        foreach (var editorState in controlEditorStates.Values)
        {
            controls.Add(editorState.Name, new Control(editorState));
        }

        return controls.Values.ToList();
    }

    private static void MergeControlEditorState(Control control, ControlEditorState controlEditorState)
    {
        control.EditorState = controlEditorState;
        if (control.Controls == null)
            return;

        foreach (var child in control.Controls)
        {
            // Find the editor state for the child by name
            var childEditorState = controlEditorState.Children.Where(c => c.Name == child.Name).FirstOrDefault();
            if (childEditorState == null)
                continue;

            MergeControlEditorState(child, childEditorState);
        }
        controlEditorState.Children = null;
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                ZipArchive?.Dispose();
                _fileStream?.Dispose();
            }

            ZipArchive = null;
            _fileStream = null;
            _canonicalEntries = null;
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
