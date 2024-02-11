// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

[DebuggerDisplay("{ToPlatformPath()}")]
public class FilePath
{
    public const int MaxFileNameLength = 60;
    public const int MaxNameLength = 50;
    private const string yamlExtension = ".fx.yaml";
    private const string editorStateExtension = ".editorstate.json";
    private readonly string[] _pathSegments;

    public FilePath(params string[] segments)
    {
        _pathSegments = segments ?? (new string[] { });
    }

    public static bool IsYamlFile(FilePath path)
    {
        return path.GetExtension() == ".yaml";
    }
    public static bool IsYamlFile(string path)
    {
        return path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
    }

    public static void VerifyFileExists(ErrorContainer errors, string fullpath)
    {
        if (!File.Exists(fullpath))
        {
            errors.BadParameter($"File not found: {fullpath}");
        }
    }

    public static void VerifyDirectoryExists(ErrorContainer errors, string fullpath)
    {
        if (!Directory.Exists(fullpath))
        {
            errors.BadParameter($"Directory not found: {fullpath}");
        }
    }

    public string ToMsAppPath()
    {
        var path = string.Join("\\", _pathSegments);

        // Some paths mistakenly start with DirectorySepChar in the msapp,
        // We replaced it with `_/` when writing, remove that now. 
        if (path.StartsWith(FileEntry.FilenameLeadingUnderscore.ToString()))
        {
            path = path.TrimStart(FileEntry.FilenameLeadingUnderscore);
        }

        return path;
    }


    public static void EnsurePathRooted(string path)
    {
        if (!Path.IsPathRooted(path))
        {
            throw new ArgumentException("Path must be a full path: " + path);
        }
    }

    /// <summary>
    /// Relative path of the file depends on the fileType. eg: Image files go into "Assets\Images\"
    /// </summary>
    public static string GetResourceRelativePath(ContentKind contentType)
    {
        switch (contentType)
        {
            case ContentKind.Image:
                return @"Assets\Images";
            case ContentKind.Audio:
                return @"Assets\Audio";
            case ContentKind.Video:
                return @"Assets\Video";
            case ContentKind.Pdf:
                return @"Assets\Pdf";
            default:
                throw new NotSupportedException("Unrecognized Content Kind for local resource");
        }
    }

    /// <summary>
    /// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
    /// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
    ///   basePath:     C:\foo
    ///   full: c:\foo\bar\hi.txt
    ///   returns "bar\hi.txt"
    /// </summary>
    /// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
    /// <param name="fullPathFile">The destination path. This is always a full path to a file.</param>
    /// <returns>The relative path or path if the paths don't share the same root.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or path is <c>null</c> or an empty string.</exception>
    /// <remarks>
    /// Want to use Path.GetRelativePath() from Net 2.1. But since we target netstandard 2.0, we need to shim it.
    /// Convert to URIs and make the relative path. 
    /// see https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
    /// For reference, see Core's impl at: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L861
    /// </remarks>
    public static string GetRelativePath(string relativeTo, string fullPathFile)
    {
        // First arg is always a path name, 2nd arg is always a directory.
        // directory is always a prefix.
        var fromUri = new Uri(AppendDirectorySeparatorChar(relativeTo));
        var toUri = new Uri(fullPathFile);

        // path can't be made relative.
        if (fromUri.Scheme != toUri.Scheme)
            return fullPathFile;

        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.InvariantCultureIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        // If there was any replacement character (black diamond with a white heart) in the original string, it gets encoded to %EF%BF%BD
        // see details here: http://www.cogsci.ed.ac.uk/~richard/utf-8.cgi?input=%EF%BF%BD&mode=char
        // special handling to add replacement character back to the original string
        relativePath = relativePath.Replace("%EF%BF%BD", "�");

        return relativePath;
    }

    private static string AppendDirectorySeparatorChar(string fullDirectory)
    {
        // Append a slash only if the path does not already have a slash.
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            return fullDirectory + Path.DirectorySeparatorChar;
        }

        return fullDirectory;
    }

    private const char EscapeChar = '%';

    private static bool DontEscapeChar(char ch)
    {
        // List of "safe" characters that aren't escaped.
        // Note that the EscapeChar must be escaped, so can't appear on this list!
        return
            (ch >= '0' && ch <= '9') ||
            (ch >= 'a' && ch <= 'z') ||
            (ch >= 'A' && ch <= 'Z') ||
            ch == '[' || ch == ']' || // common in SQL connection names
            (ch == '_') ||
            (ch == '-') ||
            (ch == '~') ||
            (ch == '.') ||
            (ch == ' '); // allow spaces, very common.
    }

    /// <summary>
    /// For writing out to a director.
    /// </summary>
    /// <param name="path"></param>
    public static string EscapeFilename(string path)
    {
        var sb = new StringBuilder();
        foreach (var ch in path)
        {
            var x = (int)ch;
            if (DontEscapeChar(ch) || x > 255)
            {
                sb.Append(ch);
            }
            else
            {
                if (x <= 255)
                {
                    sb.Append(EscapeChar);
                    sb.Append(x.ToString("x2"));
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Unescaped is backwards compat.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string UnEscapeFilename(string path)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < path.Length; i++)
        {
            var ch = path[i];
            if (ch == EscapeChar)
            {
                // Unescape
                int x;
                if (path[i + 1] == EscapeChar)
                {
                    i++;
                    x = (ToHex(path[i + 1]) * 16 * 16 * 16) +
                        (ToHex(path[i + 2]) * 16 * 16) +
                        (ToHex(path[i + 3]) * 16) +
                        ToHex(path[i + 4]);
                    i += 4;
                }
                else
                {
                    // 2 digit
                    x = (ToHex(path[i + 1]) * 16) +
                        ToHex(path[i + 2]);
                    i += 2;
                }
                sb.Append((char)x);
            }
            else
            {
                // Anything that is not explicitly escaped gets copied.
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    private static int ToHex(char ch)
    {
        if (ch >= '0' && ch <= '9')
        {
            return ch - '0';
        }
        if (ch >= 'a' && ch <= 'f')
        {
            return ch - 'a' + 10;
        }
        if (ch >= 'A' && ch <= 'F')
        {
            return ch - 'A' + 10;
        }
        throw new InvalidOperationException($"Unrecognized hex char {ch}");
    }

    /// <summary>
    /// If the name length is longer than 50, it is truncated and appended with a hash (to avoid collisions).
    /// Checks the length of the escaped name, since its possible that the length is under 60 before escaping but goes beyond 60 later.
    /// We do modulo by 1000 of the hash to limit it to 3 characters.
    /// </summary>
    /// <param name="name">The name that needs to be truncated.</param>
    /// <returns>Returns the truncated name if the escaped version of it is longer than 50 characters.</returns>
    public static string TruncateNameIfTooLong(string name)
    {
        var escapedName = EscapeFilename(name);
        if (escapedName.Length > MaxNameLength)
        {
            // limit the hash to 3 characters by doing a module by 4096 (16^3)
            var hash = (GetHash(escapedName) % 4096).ToString("x3");
            escapedName = TruncateName(escapedName, MaxNameLength - hash.Length - 1) + "_" + hash;
        }
        return escapedName;
    }

    /// <summary>
    /// Truncates a string with the given length and strips off incomplete escape characters if any.
    /// Each EscapeChar (%) must be followed by two integer values if that is not the case then it is likely that the truncation left incomplete escapes.
    /// </summary>
    /// <param name="name">The string to be truncated</param>
    /// <param name="length">The max length of the truncated string.</param>
    /// <returns>Truncated string.</returns>
    private static string TruncateName(string name, int length)
    {
        var removeTrailingCharsLength = name[length - 1] == EscapeChar ? 1 : (name[length - 2] == EscapeChar ? 2 : 0);
        return name.Substring(0, length - removeTrailingCharsLength);
    }

    /// <summary>
    /// djb2 algorithm to compute the hash of a string
    /// This must be deterministic and stable since it's used in file names
    /// </summary>
    /// <param name="str">The string for which we need to compute the hash.</param>
    /// <returns></returns>
    public static ulong GetHash(string str)
    {
        ulong hash = 5381;
        foreach (var c in str)
        {
            hash = (hash << 5) + hash + c;
        }

        return hash;
    }

    public string ToPlatformPath()
    {
        return Path.Combine(_pathSegments.Select(EscapeFilename).ToArray());
    }

    public static FilePath FromPlatformPath(string path)
    {
        if (path == null)
            return new FilePath();
        var segments = path.Split(Path.DirectorySeparatorChar).Select(UnEscapeFilename);
        return new FilePath(segments.ToArray());
    }

    public static FilePath FromMsAppPath(string path)
    {
        if (path == null)
            return new FilePath();
        var segments = path.Split('\\');
        return new FilePath(segments);
    }

    public static FilePath ToFilePath(string path)
    {
        if (path == null)
            return new FilePath();
        var segments = path.Split(Path.DirectorySeparatorChar).Select(x => x);
        return new FilePath(segments.ToArray());
    }

    public static FilePath RootedAt(string root, FilePath remainder)
    {
        var segments = new List<string>() { root };
        segments.AddRange(remainder._pathSegments);
        return new FilePath(segments.ToArray());
    }

    public FilePath Append(string segment)
    {
        var newSegments = new List<string>(_pathSegments);
        newSegments.Add(segment);
        return new FilePath(newSegments.ToArray());
    }

    public bool StartsWith(string root, StringComparison stringComparison)
    {
        return _pathSegments.Length > 0 && _pathSegments[0].Equals(root, stringComparison);
    }

    public bool HasExtension(string extension)
    {
        return _pathSegments.Length > 0 && _pathSegments.Last().EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    public string GetFileName()
    {
        if (_pathSegments.Length == 0)
            return string.Empty;
        return Path.GetFileName(_pathSegments.Last());
    }

    public string GetFileNameWithoutExtension()
    {
        if (_pathSegments.Length == 0)
            return string.Empty;
        return Path.GetFileNameWithoutExtension(_pathSegments.Last());
    }

    public string GetExtension()
    {
        if (_pathSegments.Length == 0)
            return string.Empty;
        return Path.GetExtension(EscapeFilename(_pathSegments.Last()));
    }

    public override bool Equals(object obj)
    {
        if (obj is not FilePath other)
            return false;

        if (other._pathSegments.Length != _pathSegments.Length)
            return false;

        for (var i = 0; i < other._pathSegments.Length; ++i)
        {
            if (other._pathSegments[i] != _pathSegments[i])
                return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        return ToMsAppPath().GetHashCode();
    }

    /// <summary>
    /// If there is a collision in the filename then it generates a new name for the file by appending '_1', '_2' ... to the filename.S
    /// </summary>
    /// <param name="path">The string representation of the path including filename.</param>
    /// <returns>Returns the updated path which has new filename.</returns>
    public string HandleFileNameCollisions(string path)
    {
        var suffixCounter = 0;
        var fileName = GetFileName();
        var extension = GetCustomExtension(fileName);
        var fileNameWithoutExtension = fileName.Substring(0, fileName.Length - extension.Length);
        var pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);
        while (File.Exists(path))
        {
            var filename = fileNameWithoutExtension + '_' + ++suffixCounter + extension;
            path = pathWithoutFileName + filename;
        }
        return path;
    }

    private static string GetCustomExtension(string fileName)
    {
        var extension = fileName.EndsWith(yamlExtension, StringComparison.OrdinalIgnoreCase)
                ? yamlExtension
                : fileName.EndsWith(editorStateExtension, StringComparison.OrdinalIgnoreCase)
                ? editorStateExtension
                : Path.GetExtension(fileName);
        return extension;
    }
}
