// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Utils;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;


/// <summary>
/// Represents a .msapp file.
/// </summary>
public class MsappArchive : IMsappArchive, IDisposable
{
    #region Constants

    public const string JsonFileExtension = ".json";

    #endregion

    #region Fields

    private readonly Lazy<IDictionary<string, ZipArchiveEntry>> _canonicalEntries;
    private readonly Lazy<List<Screen>> _screens;
    private bool _isDisposed;
    private readonly ILogger<MsappArchive>? _logger;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly IDeserializer? _deserializer;

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

    public MsappArchive(string path, IDeserializer? deserializer = null, ILogger<MsappArchive>? logger = null)
        : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, leaveOpen: false, deserializer, logger)
    {
    }

    public MsappArchive(Stream stream, IDeserializer? deserializer = null, ILogger<MsappArchive>? logger = null)
        : this(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null, deserializer, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, IDeserializer? deserializer = null, ILogger<MsappArchive>? logger = null)
        : this(stream, mode, leaveOpen: false, entryNameEncoding: null, deserializer, logger)
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
    /// <param name="deserializer"></param>
    /// <param name="logger"></param>
    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, IDeserializer? deserializer = null, ILogger<MsappArchive>? logger = null)
        : this(stream, mode, leaveOpen, null, deserializer, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding, IDeserializer? deserializer = null, ILogger<MsappArchive>? logger = null)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _deserializer = deserializer;
        _logger = logger;
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
                if (!canonicalEntries.TryAdd(FileUtils.NormalizePath(entry.FullName), entry))
                    _logger?.LogInformation($"Duplicate entry found in archive: {entry.FullName}");
            }

            return canonicalEntries;
        });
        _screens = new Lazy<List<Screen>>(LoadScreens);
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

    public IReadOnlyList<Screen> Screens => _screens.Value.AsReadOnly();

    #endregion

    #region Methods

    /// <inheritdoc/>
    public IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null)
    {
        _ = directoryName ?? throw new ArgumentNullException(nameof(directoryName));

        directoryName = FileUtils.NormalizePath(directoryName);

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
    public ZipArchiveEntry? GetEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            return null;

        entryName = FileUtils.NormalizePath(entryName);
        if (CanonicalEntries.TryGetValue(entryName, out var entry))
            return entry;

        return null;
    }

    /// <inheritdoc/>
    public ZipArchiveEntry CreateEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or whitespace.", nameof(entryName));

        var canonicalEntryName = FileUtils.NormalizePath(entryName);
        if (_canonicalEntries.Value.ContainsKey(canonicalEntryName))
            throw new InvalidOperationException($"Entry {entryName} already exists in the archive.");

        var entry = ZipArchive.CreateEntry(entryName);
        _canonicalEntries.Value.Add(canonicalEntryName, entry);

        return entry;
    }

    #endregion

    #region Private Methods

    private List<Screen> LoadScreens()
    {
        _logger?.LogInformation("Loading top level screens from Yaml.");

        var screens = new Dictionary<string, Screen>();
        foreach (var yamlEntry in GetDirectoryEntries(Path.Combine(Directories.SrcDirectory, Directories.ControlsDirectory), YamlUtils.YamlFileExtension))
        {
            using var textReader = new StreamReader(yamlEntry.Open());
            try
            {
                var screen = _deserializer!.Deserialize(textReader) as Screen;
                screens.Add(screen!.Name, screen);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Failed to deserialize control yaml file.", ex) { FileName = yamlEntry.FullName };
            }
        }

        _logger?.LogInformation("Loading top level controls editor state.");
        var controlEditorStates = new Dictionary<string, ControlEditorState>();
        foreach (var editorStateEntry in GetDirectoryEntries(Path.Combine(Directories.ControlsDirectory), JsonFileExtension))
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
        if (control.Controls == null)
            return;

        foreach (var child in control.Controls)
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

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (!_leaveOpen)
                {
                    ZipArchive.Dispose();
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

    public static class Directories
    {
        public const string SrcDirectory = "Src";
        public const string ControlsDirectory = "Controls";
        public const string ComponentsDirectory = "Components";
        public const string AppTestDirectory = "AppTests";
        public const string ReferencesDirectory = "References";
        public const string ResourcesDirectory = "Resources";
    }
}

