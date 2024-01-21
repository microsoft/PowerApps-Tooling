// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

/// <summary>
/// Return a full path for a temporary file, and delete it at Dispose.
/// </summary>
internal class TempFile : IDisposable
{
    public string FullPath { get; private set; }

    public TempFile()
    {
        FullPath = Path.GetTempFileName() + ".msapp";
    }

    public void Dispose()
    {
        if (FullPath != null && File.Exists(FullPath))
        {
            File.Delete(FullPath);
        }
        FullPath = null;
    }
}

/// <summary>
/// Return a unique temporary directory and delete it at Dispose
/// </summary>
internal class TempDir : IDisposable
{
    public string Dir { get; private set; }

    public TempDir()
    {
        Dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        if (Dir != null && Directory.Exists(Dir))
        {
            Directory.Delete(Dir, recursive: true);
        }
        Dir = null;
    }
}
