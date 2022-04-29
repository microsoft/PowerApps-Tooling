// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text.Json;
using System.Linq;
using System.Xml.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Abstraction over file system. 
    // Helps organize full path, relative paths
    internal class DirectoryWriter
    {
        private readonly string _directory;

        public DirectoryWriter(string directory)
        {
            this._directory = directory;
        }

        // Remove all subdirectories. This is important to avoid have previous
        // artifacts in the directories that we then pull back when round-tripping.
        public void DeleteAllSubdirs(ErrorContainer errors)
        {
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            if (ValidateSafeToDelete(errors))
            {
                foreach (var dir in Directory.EnumerateDirectories(_directory))
                {
                    if (dir.EndsWith(".git"))
                        continue;
                    Directory.Delete(dir, recursive: true);
                }
                foreach (var file in Directory.EnumerateFiles(_directory))
                {
                    if (file.StartsWith(".git"))
                        continue;
                    File.Delete(file);
                }
            }
        }

        public void WriteAllJson<T>(string subdir, FileKind kind, T obj)
        {
            var filename = FileEntry.GetFilenameForKind(kind);
            WriteAllJson(subdir, filename, obj);
        }


        public void WriteAllJson<T>(string subdir, FilePath filename, T obj)
        {
            if (Utilities.IsYamlFile(filename))
            {
                using (var tw = new StringWriter())
                {
                    YamlPocoSerializer.CanonicalWrite(tw, obj);
                    WriteAllText(subdir, filename, tw.ToString());
                }
            }
            else
            {
                var text = JsonSerializer.Serialize<T>(obj, Utilities._jsonOpts);
                text = JsonNormalizer.Normalize(text);
                WriteAllText(subdir, filename, text);
            }
        }

        // Use this if the filename is already escaped.
        public void WriteAllJson<T>(string subdir, string filename, T obj)
        {
            var text = JsonSerializer.Serialize<T>(obj, Utilities._jsonOpts);
            text = JsonNormalizer.Normalize(text);
            WriteAllText(subdir, filename, text);
        }

        public void WriteDoubleEncodedJson(string subdir, FilePath filename, string jsonStr)
        {
            if (!string.IsNullOrWhiteSpace(jsonStr) && jsonStr != "{}")
            {
                var je = JsonDocument.Parse(jsonStr).RootElement;
                WriteAllJson(subdir, filename, je);
            }
        }

        public void WriteAllXML(string subdir, FilePath filename, string xmlText)
        {
            var xml = XDocument.Parse(xmlText);
            var text = xml.ToString();
            WriteAllText(subdir, filename, text);
        }

        public void WriteAllText(string subdir, FilePath filename, string text)
        {
            string path = Path.Combine(_directory, subdir, filename.ToPlatformPath());
            EnsureFileDirExists(path);
            File.WriteAllText(path, text);
        }

        // Use this if the filename is already escaped.
        public void WriteAllText(string subdir, string filename, string text)
        {
            string path = Path.Combine(_directory, subdir, filename);

            // Check for collision so that we don't overwrite an existing file.
            if (File.Exists(path))
            {
                path = FilePath.ToFilePath(path).HandleFileNameCollisions(path);
            }

            EnsureFileDirExists(path);
            File.WriteAllText(path, text);
        }

        public void WriteAllBytes(string subdir, FilePath filename, byte[] bytes)
        {
            string path = Path.Combine(_directory, subdir, filename.ToPlatformPath());
            EnsureFileDirExists(path);
            File.WriteAllBytes(path, bytes);
        }

        // System.IO.File's built in functions fail if the directory doesn't already exist. 
        // Must pre-create it before writing. 
        public static void EnsureFileDirExists(string path)
        {
            var errors = new ErrorContainer();

            if (string.IsNullOrEmpty(path))
            {
                errors.BadParameter("Path to file directory cannot be null or empty.");
                throw new DocumentException();
            }

            System.IO.FileInfo file = new System.IO.FileInfo(path);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
        }

        /// <summary>
        /// Checks if the file exists in the specified subdirectory.
        /// </summary>
        /// <param name="subdir">The subdirectory</param>
        /// <param name="filename">Name of  the file.</param>
        /// <returns>True if the file exists.</returns>
        public bool FileExists(string subdir, string filename)
        {
            string path = Path.Combine(_directory, subdir, filename);
            return File.Exists(path);
        }

        /// <summary>
        /// Returns true if it's either an empty directory or it contains CanvasManifest.json file.
        /// </summary>
        /// <returns></returns>
        private bool ValidateSafeToDelete(ErrorContainer errors)
        {
            if (Directory.EnumerateFiles(_directory).Any() && !File.Exists(Path.Combine(_directory, "CanvasManifest.json")))
            {
                errors.BadParameter("Must provide path to either empty directory or a directory where the app was previously unpacked.");
                throw new DocumentException();
            }
            return true;
        }
    }

    // Abstraction over file system. 
    // Helps organize full path, relative paths
    internal class DirectoryReader
    {
        private readonly string _directory;

        public DirectoryReader(string directory)
        {
            this._directory = directory;
        }

        // A file that can be read. 
        public class Entry
        {
            private string _fullpath;
            public FileKind Kind;
            internal string _relativeName;

            public Entry(string fullPath)
            {
                this._fullpath = fullPath;
            }

            public SourceLocation SourceSpan
            {
                get
                {
                    return SourceLocation.FromFile(this._fullpath);
                }
            }

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
                    RawBytes = File.ReadAllBytes(this._fullpath)
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
}
