// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.IO;
using System.Text.Json;
using System.Linq;

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
        public void DeleteAllSubdirs()
        {
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            foreach (var dir in Directory.EnumerateDirectories(_directory))
            {
                if (dir.EndsWith(".git"))
                    continue;
                Directory.Delete(dir, recursive: true);
            }
        }

        public void WriteAllJson<T>(string subdir, FileKind kind, T obj)
        {
            string filename = FileEntry.GetFilenameForKind(kind);
            WriteAllJson(subdir, filename, obj);
        }


        public void WriteAllJson<T>(string subdir, string filename, T obj)
        {
            var text = JsonSerializer.Serialize<T>(obj, Utility._jsonOpts);
            text = JsonNormalizer.Normalize(text);
            WriteAllText(subdir, filename, text);
        }

        public void WriteDoubleEncodedJson(string subdir, string filename, string jsonStr)
        {
            if (!string.IsNullOrWhiteSpace(jsonStr) && jsonStr != "{}")
            {
                var je = JsonDocument.Parse(jsonStr).RootElement;

                string path = Path.Combine(_directory, subdir, filename);
                this.WriteAllJson(subdir, filename, je);
            }
        }

        public void WriteAllText(string subdir, string filename, string text)
        {
            string path = Path.Combine(_directory, subdir, Utility.EscapeFilename(filename));
            EnsureFileDirExists(path);
            File.WriteAllText(path, text);
        }

        public void WriteAllBytes(string subdir, string filename, byte[] bytes)
        {
            string path = Path.Combine(_directory, subdir, Utility.EscapeFilename(filename));
            EnsureFileDirExists(path);
            File.WriteAllBytes(path, bytes);
        }

        // System.IO.File's built in functions fail if the directory doesn't already exist. 
        // Must pre-create it before writing. 
        private static void EnsureFileDirExists(string path)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
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
            internal string _relativeDir;
            internal string _fileName;

            public string RelativeName => Path.Combine(_relativeDir, _fileName);

            public Entry(string fullPath, string relativePath)
            {
                _fullpath = fullPath;
                _relativeDir = Path.GetDirectoryName(relativePath);
                _fileName = Utility.UnEscapeFilename(Path.GetFileName(relativePath));
            }

            // FileEntry is the same structure we get back from a Zip file. 
            public FileEntry ToFileEntry()
            {
                return new FileEntry
                {
                    Name = Path.Combine(_relativeDir, _fileName),
                    RawBytes = File.ReadAllBytes(_fullpath)
                };
            }

            public T ToObject<T>()
            {
                var str = File.ReadAllText(_fullpath);
                return JsonSerializer.Deserialize<T>(str, Utility._jsonOpts);
            }

            public string GetContents()
            {
                return File.ReadAllText(_fullpath);
            }
        }      

        // Returns file entries. 
        public Entry[] EnumerateFiles(string subdir, string pattern = "*")
        {
            var root = Path.Combine(_directory, subdir);

            if (!Directory.Exists(root))
            {
                return new Entry[0];
            }

            var fullPaths = Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories);

            var entries = from fullPath in fullPaths
                          let relativePath = Utility.GetRelativePath(fullPath, root)
                          select new Entry(fullPath, relativePath)
                          {
                              Kind = FileEntry.TriageKind(relativePath)
                          };

            return entries.ToArray();
        }
    }
}
