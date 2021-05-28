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
        public const int MAX_PATH = 260;
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

        public string ToPlatformPath()
        {
            return Path.Combine(_pathSegments.Select(Utilities.EscapeFilename).ToArray());
        }

        /// <summary>
        /// Checks if the path length is more than MAX_PATH (260)
        /// then it truncates the path, and adds a hash to the filename while retaining the file extension.
        /// </summary>
        /// <param name="path">Full path of the file.</param>
        /// <returns></returns>
        public static string ToValidPath(string path)
        {
            if (path.Length > MAX_PATH)
            {
                var extension = path.EndsWith(yamlExtension)
                    ? yamlExtension
                    : path.EndsWith(editorStateExtension)
                    ? editorStateExtension
                    : Path.GetExtension(path);

                var hash = string.Format("{0:X}", path.GetHashCode());
                path = path.Substring(0, MAX_PATH - (extension.Length + hash.Length));
                path = string.Concat(path, hash, extension);
            }
            return path;
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

    }
}
