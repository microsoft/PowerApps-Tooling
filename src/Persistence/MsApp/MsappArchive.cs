// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Represents a .msapp file.
/// </summary>
public partial class MsappArchive : IMsappArchive, IDisposable
{
    private const string HeaderFileName = "Header.json";

    private static readonly JsonSerializerOptions JsonDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, // We don't want to fail if there are extra properties in the json
    };

    private ZipArchive? _zipArchive;
    private Dictionary<string, ZipArchiveEntry>? _canonicalEntries;
    private HeaderJson? _header;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="mode"></param>
    /// <param name="leaveOpen">true to leave the stream open after the System.IO.Compression.ZipArchive object is disposed; otherwise, false</param>
    /// <param name="entryNameEncoding"></param>
    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen = false, Encoding? entryNameEncoding = null)
    {
        _zipArchive = new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
    }

    /// <inheritdoc/>
    public ZipArchive ZipArchive => _zipArchive ?? throw new ObjectDisposedException(nameof(MsappArchive));

    /// <summary>
    /// Total sum of decompressed sizes of all entries in the archive.
    /// </summary>
    public long DecompressedSize => ZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.Length);

    /// <summary>
    /// Total sum of compressed sizes of all entries in the archive.
    /// </summary>
    public long CompressedSize => ZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.CompressedLength);

    private HeaderJson Header => _header ??= LoadHeader();

    public Version MSAppStructureVersion => Header.MSAppStructureVersion;

    public Version DocVersion => Header.DocVersion;

    private Dictionary<string, ZipArchiveEntry> InnerCanonicalEntries
    {
        get
        {
            if (_canonicalEntries is null)
            {
                EnsureNotDisposed();

                var canonicalEntries = new Dictionary<string, ZipArchiveEntry>();

                // In Create mode, we don't have access to the Entries, so we create it as empty.
                // This should be fine, as this property is only used when adding entries.
                if (ZipArchive.Mode != ZipArchiveMode.Create)
                {
                    foreach (var entry in ZipArchive.Entries)
                    {
                        var canonicalizedPath = CanonicalizePath(entry.FullName);
                        if (!canonicalEntries.TryAdd(canonicalizedPath, entry))
                        {
                            throw new InvalidDataException($"Duplicate canonicalized entry found in zip archive. EntryFullName: '{entry.FullName}'; CanonicalizedPath: '{canonicalizedPath}';");
                        }
                    }
                }

                _canonicalEntries = canonicalEntries;
            }

            return _canonicalEntries;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, ZipArchiveEntry> CanonicalEntries()
    {
        return InnerCanonicalEntries.AsReadOnly();
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_zipArchive is null, this);
    }

    /// <inheritdoc/>
    public bool DoesEntryExist(string entryPath)
    {
        _ = entryPath ?? throw new ArgumentNullException(nameof(entryPath));

        return InnerCanonicalEntries.ContainsKey(CanonicalizePath(entryPath));
    }

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null, bool recursive = true)
    {
        directoryName = CanonicalizePath(directoryName).TrimEnd('/');

        foreach (var kvp in InnerCanonicalEntries)
        {
            // Do not return directories which some zip implementations include as entries
            if (kvp.Key.EndsWith('/'))
                continue;

            if (directoryName != string.Empty && !kvp.Key.StartsWith(directoryName + '/', StringComparison.InvariantCulture))
                continue;

            // If not recursive, skip subdirectories
            if (!recursive && kvp.Key.IndexOf('/', directoryName.Length == 0 ? 0 : directoryName.Length + 1) > 0)
                continue;

            if (extension != null && !kvp.Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return kvp.Value;
        }
    }

    /// <inheritdoc/>
    public ZipArchiveEntry? GetEntryOrDefault(string entryName)
    {
        return TryGetEntry(entryName, out var entry) ? entry : null;
    }

    /// <inheritdoc/>
    public bool TryGetEntry(string entryName, [MaybeNullWhen(false)] out ZipArchiveEntry zipArchiveEntry)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        if (string.IsNullOrWhiteSpace(entryName))
        {
            zipArchiveEntry = null;
            return false;
        }

        return InnerCanonicalEntries.TryGetValue(CanonicalizePath(entryName), out zipArchiveEntry);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);

        var canonicalEntryName = CanonicalizePath(entryName);
        if (InnerCanonicalEntries.ContainsKey(canonicalEntryName))
            throw new InvalidOperationException($"Entry {entryName} already exists in the archive.");

        var entry = ZipArchive.CreateEntry(entryName);
        InnerCanonicalEntries.Add(canonicalEntryName, entry);

        return entry;
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

    /// <summary>
    /// Canonicalizes an entry path to a value used in the canonical entries dictionary (<see cref="IMsappArchive.CanonicalEntries"/>).
    /// It removes leading and trailing slashes, converts backslashes to forward slashes, and makes the path lowercase.
    /// </summary>
    public static string CanonicalizePath(string? path)
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

    private HeaderJson LoadHeader()
    {
        var entry = GetRequiredEntry(HeaderFileName);
        var header = DeserializeMsappJsonFile<HeaderJson>(entry);
        return header;
    }

    /// <inheritdoc/>
    public void AddGitIgnore()
    {
        if (ZipArchive.Mode == ZipArchiveMode.Read)
            throw new InvalidOperationException("Cannot add .gitignore entry when the archive is opened in Read mode.");
        if (DoesEntryExist(".gitignore"))
            throw new InvalidOperationException("Cannot add .gitignore entry when it already exists.");

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
            return JsonSerializer.Deserialize<T>(entry.Open(), JsonDeserializeOptions)
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

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _zipArchive != null)
        {
            // ZipArchive.Dispose() finishes writing the zip file with it's current contents when opened in Create or Update mode.
            // It also disposes the underlying stream unless leaveOpen was set to true.
            _zipArchive.Dispose();
            _zipArchive = null;
            _canonicalEntries = null;
            _header = null;
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
