// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

/// <summary>
/// Abstraction over file system.
/// Helps organize full path, relative paths
/// </summary>
internal class DirectoryReader(string directory)
{
    // A file that can be read.
    public class Entry(string fullPath)
    {
        public FileKind Kind;
        internal string _relativeName;

        public SourceLocation SourceSpan => SourceLocation.FromFile(fullPath);

        // FileEntry is the same structure we get back from a Zip file.
        public FileEntry ToFileEntry()
        {
            // Some paths mistakenly start with DirectorySepChar in the msapp,
            // We replaced it with `_/` when writing, remove that now.
            if (_relativeName.StartsWith(FileEntry.FilenameLeadingUnderscore.ToString()))
                _relativeName = _relativeName.TrimStart(FileEntry.FilenameLeadingUnderscore);
            return new FileEntry
            {
                Name = FilePath.FromPlatformPath(_relativeName),
                RawBytes = File.ReadAllBytes(fullPath)
            };
        }

        public T ToObject<T>()
        {
            if (FilePath.IsYamlFile(fullPath))
            {
                using (var textReader = new StreamReader(fullPath))
                {
                    var obj = YamlPocoSerializer.Read<T>(textReader);
                    return obj;
                }
            }
            else
            {
                var str = File.ReadAllText(fullPath);
                return JsonSerializer.Deserialize<T>(str, JsonExtensions._jsonOpts);
            }
        }

        public string GetContents()
        {
            return File.ReadAllText(fullPath);
        }
    }

    // Returns file entries.
    public Entry[] EnumerateFiles(string subdir, string pattern = "*", bool searchSubdirectories = true)
    {
        var root = Path.Combine(directory, subdir);

        if (!Directory.Exists(root))
        {
            return [];
        }

        var fullPaths = Directory.EnumerateFiles(root, pattern, searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        var entries = from fullPath in fullPaths
                      let relativePath = FilePath.GetRelativePath(root, fullPath)
                      select new Entry(fullPath)
                      {
                          _relativeName = relativePath,
                          Kind = FileEntry.TriageKind(FilePath.FromPlatformPath(relativePath))
                      };

        return [.. entries];
    }

    // Returns subdirectories.
    public DirectoryReader[] EnumerateDirectories(string subdir, string pattern = "*", bool searchSubdirectories = false)
    {
        var root = Path.Combine(directory, subdir);

        if (!Directory.Exists(root))
        {
            return [];
        }

        var fullPaths = Directory.EnumerateDirectories(root, pattern, searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        var entries = from fullPath in fullPaths
                      let relativePath = FilePath.GetRelativePath(root, fullPath)
                      select new DirectoryReader(fullPath);

        return [.. entries];
    }
}
