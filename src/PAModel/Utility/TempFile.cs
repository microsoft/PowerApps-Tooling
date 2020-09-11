// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// Return a full path for a temporary file, and delete it at Dispose.
    /// </summary>
    public class TempFile : IDisposable
    {
        public string FullPath { get; private set; }

        public TempFile()
        {
            this.FullPath = Path.GetTempFileName() + ".msapp";
        }

        public void Dispose()
        {
            if (this.FullPath != null && File.Exists(this.FullPath))
            {
                File.Delete(this.FullPath);
            }
            this.FullPath = null;
        }
    }

    /// <summary>
    /// Return a unique temporary directory and delete it at Dispose
    /// </summary>
    public class TempDir : IDisposable
    {
        public string Dir { get; private set; }

        public TempDir()
        {
            this.Dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        public void Dispose()
        {
            if (this.Dir != null && Directory.Exists(this.Dir))
            {
                Directory.Delete(this.Dir, recursive: true);
            }
            this.Dir = null;
        }
    }
}
