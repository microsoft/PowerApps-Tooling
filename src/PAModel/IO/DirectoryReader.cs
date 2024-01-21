// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

/// <summary>
/// Abstraction over file system. 
/// Helps organize full path, relative paths
/// </summary>
internal class DirectoryReader
{
    private readonly string _directory;

    public DirectoryReader(string directory)
    {
        _directory = directory;
    }

    // A file that can be read. 
    public class Entry
    {
        private readonly string _fullpath;
        public FileKind Kind;
        internal string _relativeName;

        public Entry(string fullPath)
        {
            _fullpath = fullPath;
        }

        public SourceLocation SourceSpan => SourceLocation.FromFile(_fullpath);

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
                RawBytes = File.ReadAllBytes(_fullpath)
            };
        }

        public T ToObject<T>()
        {
            if (Utilities.IsYamlFile(_fullpath))
            {
                using (var textReader = new StreamReader(_fullpath))
                {
                    var obj = YamlPocoSerializer.Read<T>(textReader);
                    return obj;
                }
            }
            else
            {
                var str = File.ReadAllText(_fullpath);
                return JsonSerializer.Deserialize<T>(str, Utilities._jsonOpts);
            }
        }

        public string GetContents()
        {
            return File.ReadAllText(_fullpath);
        }
    }

    // Returns file entries. 
    public Entry[] EnumerateFiles(string subdir, string pattern = "*", bool searchSubdirectories = true)
    {
        var root = Path.Combine(_directory, subdir);

        if (!Directory.Exists(root))
        {
            return new Entry[0];
        }

        var fullPaths = Directory.EnumerateFiles(root, pattern, searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        var entries = from fullPath in fullPaths
                      let relativePath = Utilities.GetRelativePath(root, fullPath)
                      select new Entry(fullPath)
                      {
                          _relativeName = relativePath,
                          Kind = FileEntry.TriageKind(FilePath.FromPlatformPath(relativePath))
                      };

        return entries.ToArray();
    }

    // Returns subdirectories. 
    public DirectoryReader[] EnumerateDirectories(string subdir, string pattern = "*", bool searchSubdirectories = false)
    {
        var root = Path.Combine(_directory, subdir);

        if (!Directory.Exists(root))
        {
            return new DirectoryReader[0];
        }

        var fullPaths = Directory.EnumerateDirectories(root, pattern, searchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        var entries = from fullPath in fullPaths
                      let relativePath = Utilities.GetRelativePath(root, fullPath)
                      select new DirectoryReader(fullPath);

        return entries.ToArray();
    }
}
