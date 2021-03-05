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

        public bool StartsWith(string root, StringComparison stringComparison)
        {
            return _pathSegments.Length > 0 && _pathSegments[0].Equals(root, stringComparison);
        }

        public bool HasExtension(string extension)
        {
            return _pathSegments.Length > 0 && _pathSegments.Last().EndsWith(extension, StringComparison.OrdinalIgnoreCase);
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
