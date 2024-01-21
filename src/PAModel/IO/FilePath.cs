// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

[DebuggerDisplay("{ToPlatformPath()}")]
public class FilePath
{
    public const int MaxFileNameLength = 60;
    private const string yamlExtension = ".fx.yaml";
    private const string editorStateExtension = ".editorstate.json";
    private readonly string[] _pathSegments;

    public FilePath(params string[] segments)
    {
        _pathSegments = segments ?? (new string[] { });
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

    public string ToPlatformPath()
    {
        return Path.Combine(_pathSegments.Select(Utilities.EscapeFilename).ToArray());
    }

    public static FilePath FromPlatformPath(string path)
    {
        if (path == null)
            return new FilePath();
        var segments = path.Split(Path.DirectorySeparatorChar).Select(Utilities.UnEscapeFilename);
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
        return Path.GetExtension(Utilities.EscapeFilename(_pathSegments.Last()));
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
