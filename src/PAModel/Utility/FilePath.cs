// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Utility
{
    internal class FilePath
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
            return string.Join("\\", _pathSegments);
        }

        /// <summary>
        /// Performs escaping on the path, and also truncates the filename to a max length of 60, if longer.
        /// </summary>
        /// <returns></returns>
        public string ToPlatformPath()
        {
            var originalFileName = this.GetFileName();
            var newFileName = GetTruncatedFileNameIfTooLong(this.GetFileName());
            var remainingPath =  Path.Combine(_pathSegments.Where(x => x != originalFileName).Select(Utilities.EscapeFilename).ToArray());
            return Path.Combine(remainingPath, newFileName);
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
            return Path.GetExtension(_pathSegments.Last());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FilePath other))
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
        /// If the file name length is longer than 60, it is truncated and appeded with a hash (to avoid collisions).
        /// Checks the length of the escaped file name, since its possible that the length is under 60 before excaping but goes beyond 60 later.
        /// We do module by 1000 of the hash to limit it to 3 characters.
        /// </summary>
        /// <returns></returns>
        private string GetTruncatedFileNameIfTooLong(string fileName)
        {
            var newFileName = Utilities.EscapeFilename(fileName);
            if (newFileName.Length > 60)
            {
                var extension = newFileName.EndsWith(yamlExtension)
                    ? yamlExtension
                    : newFileName.EndsWith(editorStateExtension)
                    ? editorStateExtension
                    : Path.GetExtension(newFileName);

                // limit the hash to 3 characters by doing a module by 1000
                var hash = (GetHash(newFileName) % 1000).ToString("x3");
                newFileName = newFileName.Substring(0, MaxFileNameLength - extension.Length - hash.Length) + hash + extension;
            }
            return newFileName;
        }

        /// <summary>
        /// djb2 algorithm to compute the hash of a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private ulong GetHash(string str)
        {
            ulong hash = 5381;
            foreach(char c in str)
            {
                hash = ((hash << 5) + hash) + c;
            }

            return hash;
        }
    }
}
