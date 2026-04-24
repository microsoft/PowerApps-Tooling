// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Represents a normalized entry path in a <see cref="PaArchive"/>.
/// These paths are normalized to be friendly with the <see cref="System.IO.Path"/> APIs for the current platform.
/// <br/>
/// - Directory entries (which end with a separator char) are maintained.<br/>
/// - Segment separators are normalized to <see cref="Path.DirectorySeparatorChar"/>.<br/>
/// - Segments are trimmed of whitespace. Reason: Windows trips on leading/trailing whitespace of directory and file names.<br/>
/// - Whitespace and empty segments are removed. Reason: To remove superfluous segments that don't add meaning to the path and can cause issues on some platforms (e.g. Windows).<br/>
/// - empty, whitespace or having all segments be whitespace or empty result in the root path (empty string) being returned.<br/>
/// - Leading separator chars are removed. eg. "/dir/file" becomes "dir/file". Reason: Security. Remove accidental rooting when combining with a target disk path.<br/>
/// </summary>
[DebuggerDisplay("FullName: {FullName}")]
public partial class PaArchivePath : IEquatable<PaArchivePath>, IEquatable<string>
{
    public static readonly PaArchivePath Root = new(string.Empty);

    private static readonly StringComparison DefaultStringComparison = StringComparison.OrdinalIgnoreCase; // Ordinal is the most secure comparison to use for path comparisons
    private static readonly StringComparer DefaultStringComparer = StringComparer.OrdinalIgnoreCase;

    private const char SeparatorCharWindows = '\\';
    private const char SeparatorCharUnix = '/';
    private static readonly char[] AllSeparatorChars = [SeparatorCharWindows, SeparatorCharUnix];

    /// <summary>
    /// Used for testing only.
    /// </summary>
    internal static char[] GetAllSeparatorChars() => [.. AllSeparatorChars];

    /// <summary>
    /// Paths for a <see cref="PaArchive"/> should not contain any of these characters.
    /// This list includes chars which are not valid for any platform's paths or file names, along with some symbols which can cause some issues in different applications.
    /// </summary>
    public static char[] GetInvalidPathChars() => [
        // ASCII Control Chars:
        '\0', // NULL
        (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31,

        // non-control chars not allowed in Windows filenames are added here:
        // We are explicitly only wanting to support valid relative paths, so we don't allow ':' for drive letters etc.
        '\"', '<', '>', '|',
        ':', '*', '?',
        ];

#if NET8_0_OR_GREATER
    private static readonly System.Buffers.SearchValues<char> _invalidPathChars = System.Buffers.SearchValues.Create(GetInvalidPathChars());
#else
    private static readonly char[] _invalidPathChars = GetInvalidPathChars();
#endif

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="fullName"/> is not valid for a <see cref="PaArchive"/>.</exception>
    public PaArchivePath(string? fullName)
    {
        if (!TryValidatePath(fullName, out var validatedFullName, out var name, out var reason))
            throw new ArgumentException(GetInvalidReasonExceptionMessage(reason.Value), nameof(fullName));

        FullName = validatedFullName;
        Name = name;
    }

    /// <summary>
    /// Used to efficiently create derived instances w/o needing to reparse, as they're already normalized.
    /// </summary>
    private PaArchivePath(string validFullName, string name)
    {
        FullName = validFullName;
        Name = name;
    }

    /// <summary>
    /// The normalized full name for an entry.
    /// Zip-related archives expect entries to use the current platform's directory separator char, and this class normalizes to that end.
    /// This path will end with <see cref="Path.DirectorySeparatorChar"/> when this instance represents a directory path.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// The file or directory name for this path.
    /// This will be string.Empty for root entry path.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indicates whether this instance represents a path to the root of the archive.
    /// This can happen when an entry has whitespace and separators only, which are ignored.
    /// </summary>
    public bool IsRoot => FullName.Length == 0;

    /// <summary>
    /// Indicates whether this instance represents a directory entry, which some implementations support.
    /// </summary>
    public bool IsDirectory => FullName.EndsWith(Path.DirectorySeparatorChar);

    public static implicit operator string(PaArchivePath path) => path.FullName;

    public static bool operator ==(PaArchivePath? left, PaArchivePath? right)
    {
        return EqualityComparer<PaArchivePath>.Default.Equals(left!, right!);
    }

    public static bool operator !=(PaArchivePath? left, PaArchivePath? right)
    {
        return !(left == right);
    }

    public override string ToString() => FullName;

    public override int GetHashCode()
    {
        return DefaultStringComparer.GetHashCode(FullName);
    }

    public override bool Equals(object? obj)
    {
        // For this overload, we don't support strings. Caller should use strong-type value at that point.
        return Equals(obj as PaArchivePath);
    }

    public bool Equals(PaArchivePath? other)
    {
        return other is not null
            && DefaultStringComparer.Equals(FullName, other.FullName);
    }

    /// <summary>
    /// Performs case-insensitive match, without normalizing the intput string.
    /// This means this overload should only be used when the input is known to already be normalized.
    /// </summary>
    /// <returns><c>true</c> if this instance is case-insensitive match with the input string; Otherwise <c>false</c>.</returns>
    public bool Equals(string? other)
    {
        return FullName.Equals(other, DefaultStringComparison);
    }

    public bool ContainsPath(string path, bool nonRecursive = false)
    {
        return ContainsPath(new PaArchivePath(path), nonRecursive: nonRecursive);
    }

    public bool ContainsPath(PaArchivePath path, bool nonRecursive = false)
    {
        if (!IsRoot && !IsDirectory)
            throw new InvalidOperationException($"{nameof(ContainsPath)} should only be called on a directory or root path.");

        // When the search directory is the root, all entry paths are trivially under it
        if (IsRoot)
        {
            return !nonRecursive
                || !path.FullName.Contains(Path.DirectorySeparatorChar);
        }

        // If the entry is the root, then there's no way to match any non-root directory path
        if (path.IsRoot)
            return false;

        // Is it under the directory?
        if (!path.FullName.StartsWith(FullName, DefaultStringComparison))
            return false;

        // When not recursive, make sure the entry is an immediate child (no additional separators)
        return !nonRecursive
             || path.FullName.IndexOf(Path.DirectorySeparatorChar, startIndex: FullName.Length) == -1;
    }

    public bool MatchesFileExtension(string extension)
    {
        return !IsRoot && !IsDirectory
            && Name.EndsWith(extension, DefaultStringComparison);
    }

    /// <summary>
    /// Creates a new normalized path by combining the paths.
    /// Similar to <see cref="Path.Combine(string, string)"/>.
    /// </summary>
    /// <returns>A new normalized path.</returns>
    public PaArchivePath Combine(PaArchivePath otherPath)
    {
        if (IsRoot)
            return otherPath;

        if (otherPath.IsRoot)
            return this;

        // Note: We don't need to work worry if we are a directory, as the Path.Combine will do this already
        return new(validFullName: Path.Combine(FullName, otherPath.FullName), otherPath.Name);
    }

    public static bool TryParse(string? fullName, [NotNullWhen(true)] out PaArchivePath? path, [NotNullWhen(false)] out PaArchivePathInvalidReason? reason)
    {
        if (!TryValidatePath(fullName, out var validatedFullName, out var name, out reason))
        {
            path = null;
            return false;
        }

        path = new(validatedFullName, name);
        return true;
    }

    /// <summary>
    /// Gets a friendly message for why a path is invalid, which can be used in exceptions.
    /// </summary>
    /// <param name="reason">The reason why the path is invalid.</param>
    /// <returns>A friendly message describing why the path is invalid.</returns>
    public static string GetInvalidReasonExceptionMessage(PaArchivePathInvalidReason reason)
    {
        return reason switch
        {
            PaArchivePathInvalidReason.InvalidPathChars => $"The path contains invalid characters. Example invalid chars are ASCII control characters and {string.Join(string.Empty, GetInvalidPathChars().Where(static c => !Char.IsControl(c)))}.",
            PaArchivePathInvalidReason.WhitespaceOnlySegment => "The path contains a segment that is whitespace only, which is not allowed.",
            PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace => "The path contains a segment with leading or trailing whitespace, which is not allowed.",
            PaArchivePathInvalidReason.IllegalSegment => "The path contains an illegal segment (e.g. '.' or '..'), which is not allowed.",
            _ => $"The path is invalid. (Reason: {reason})"
        };
    }

    /// <summary>
    /// Creates a new instance which represents a directory with the specified path.
    /// </summary>
    /// <param name="fullName">This needs not end with a separator character. Empty or null will result in returning <see cref="Root"/>.</param>
    public static PaArchivePath AsDirectoryOrRoot(string? fullName)
    {
        if (!TryValidatePath(fullName, out var validatedFullName, out var name, out var reason, forceAsDirectory: true))
            throw new ArgumentException(GetInvalidReasonExceptionMessage(reason.Value), nameof(fullName));

        if (validatedFullName.Length == 0)
            return Root;

        return new PaArchivePath(validatedFullName, name);
    }

    public static PaArchivePath ParseArgument(string? fullName, [CallerArgumentExpression(nameof(fullName))] string? paramName = null)
    {
        if (!TryParse(fullName, out var path, out var invalidReason))
        {
            throw new ArgumentException(GetInvalidReasonExceptionMessage(invalidReason.Value), paramName: paramName);
        }

        return path;
    }

    /// <summary>
    /// This method should ONLY be used for unit tests.
    /// It allows us to simulate creation of an instance of <see cref="PaArchivePath"/> which has a characteristic which
    /// could cause problems.
    /// </summary>
    [Obsolete("Only use within tests for this library")]
    internal static PaArchivePath TestOnly_CreateInvalidPath(string invalidFullName)
    {
        ThrowIfNullOrWhiteSpace(invalidFullName);

        if (TryValidatePath(invalidFullName, out _, out _, out _))
            throw new ArgumentException($"This method should only be used to create known invalid paths");

        // At a minimum, normalize path separators to current platform:
        invalidFullName = invalidFullName
            .Replace(SeparatorCharUnix, Path.DirectorySeparatorChar)
            .Replace(SeparatorCharWindows, Path.DirectorySeparatorChar);

        return new PaArchivePath(invalidFullName, Path.GetFileName(invalidFullName));
    }

    private static bool TryValidatePath(
        string? origFullName,
        [NotNullWhen(true)] out string? validFullName,
        [NotNullWhen(true)] out string? name,
        [NotNullWhen(false)] out PaArchivePathInvalidReason? reason,
        bool forceAsDirectory = false)
    {
        validFullName = null;
        name = null;
        reason = null;

        if (StringTfmAdapter.IsNullOrEmpty(origFullName))
        {
            // Root
            validFullName = name = string.Empty;
            return true;
        }

        var origFullNameSpan = origFullName.AsSpan();
        if (origFullNameSpan.IndexOfAny(_invalidPathChars) >= 0)
        {
            reason = PaArchivePathInvalidReason.InvalidPathChars;
            return false;
        }

        // Some implementations allow adding folder entries.
        // These paths should end with a directory separator.
        // This should only be done on the unmodified full path
        var isDirectoryPath = origFullName.EndsWith(SeparatorCharWindows) || origFullName.EndsWith(SeparatorCharUnix);

        // TODO: net9 provides simpler usage of Split
        // Get the non-empty segments, split by segments on any of the known separator chars
        var segments = origFullName.Split(AllSeparatorChars, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            // If we have no non-empty segments, then treat as the root path
            validFullName = name = string.Empty;
            return true;
        }

        reason = ValidateSegments(segments);
        if (reason is not null)
        {
            return false;
        }

        // Compose final relative path
        validFullName = StringTfmAdapter.Join(Path.DirectorySeparatorChar, segments);
        if (isDirectoryPath || forceAsDirectory)
            validFullName += Path.DirectorySeparatorChar;

        // The file/directory name is alway the last segment
        name = segments[^1];
        return true;

        static PaArchivePathInvalidReason? ValidateSegments(string[] segments)
        {
            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                    return PaArchivePathInvalidReason.WhitespaceOnlySegment;
                if (segment == ".." || segment == "."
                    // In Windows, a filename cannot end with a period
                    || segment[^1] == '.')
                    return PaArchivePathInvalidReason.IllegalSegment;
                if (segment.Trim().Length != segment.Length)
                    return PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace;
            }

            return null;
        }
    }
}
