using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace PAModel
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

        public void WriteAllJson<T>(string subdir, FileKind kind, T obj)
        {
            string filename = FileEntry.GetFilenameForKind(kind);
            var text = JsonSerializer.Serialize<T>(obj, Utility._jsonOpts);
            WriteAllText(subdir, filename, text);
        }


        public void WriteAllJson<T>(string subdir, string filename, T obj)
        {
            var text = JsonSerializer.Serialize<T>(obj, Utility._jsonOpts);
            WriteAllText(subdir, filename, text);
        }

        public void WriteDoubleEncodedJson(string subdir, string filename, string jsonStr)
        {
            if (jsonStr != "{}")
            {
                var je = JsonDocument.Parse(jsonStr).RootElement;

                string path = Path.Combine(_directory, subdir, filename);
                this.WriteAllJson(subdir, filename, je);
            }
        }

        public void WriteAllText(string subdir, string filename, string text)
        {
            string path = Path.Combine(_directory, subdir, filename);
            Utility.EnsureFileDirExists(path);
            File.WriteAllText(path, text);
        }

        public void WriteAllBytes(string subdir, string filename, byte[] bytes)
        {
            string path = Path.Combine(_directory, subdir, filename);
            Utility.EnsureFileDirExists(path);
            File.WriteAllBytes(path, bytes);
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
            internal string _fullpath;
            public FileKind Kind;
            internal string _relativeName;

            // FileEntry is the same structure we get back from a Zip file. 
            public FileEntry ToFileEntry()
            {
                return new FileEntry
                {
                    Name = this._relativeName.Replace('/', '\\'),
                    RawBytes = File.ReadAllBytes(this._fullpath)
                };
            }

            public T ToObject<T>()
            {
                var str = File.ReadAllText(_fullpath);
                return JsonSerializer.Deserialize<T>(str, Utility._jsonOpts);
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
                          select new Entry
                          {
                              _relativeName = relativePath,
                              _fullpath = fullPath,
                              Kind = FileEntry.TriageKind(relativePath)
                          };

            return entries.ToArray();
        }
    }
}