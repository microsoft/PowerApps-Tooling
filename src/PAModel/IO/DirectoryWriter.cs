// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.JsonConverters;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;

namespace Microsoft.PowerPlatform.Formulas.Tools.IO;

/// <summary>
/// Abstraction over file system.
/// Helps organize full path, relative paths
/// </summary>
public class DirectoryWriter(string directory)
{
    // Remove all subdirectories. This is important to avoid have previous
    // artifacts in the directories that we then pull back when round-tripping.
    public void DeleteAllSubdirs(ErrorContainer errors)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        if (ValidateSafeToDelete(errors))
        {
            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                if (dir.EndsWith(".git"))
                    continue;
                Directory.Delete(dir, recursive: true);
            }
            foreach (var file in Directory.EnumerateFiles(directory))
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
        if (FilePath.IsYamlFile(filename))
        {
            using (var tw = new StringWriter())
            {
                YamlPocoSerializer.CanonicalWrite(tw, obj);
                WriteAllText(subdir, filename, tw.ToString());
            }
        }
        else
        {
            var text = JsonSerializer.Serialize(obj, JsonExtensions._jsonOpts);
            text = JsonNormalizer.Normalize(text);
            WriteAllText(subdir, filename, text);
        }
    }

    // Use this if the filename is already escaped.
    public void WriteAllJson<T>(string subdir, string filename, T obj)
    {
        var text = JsonSerializer.Serialize(obj, JsonExtensions._jsonOpts);
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
        var path = Path.Combine(directory, subdir, filename.ToPlatformPath());
        EnsureFileDirExists(path);
        File.WriteAllText(path, text);
    }

    // Use this if the filename is already escaped.
    public void WriteAllText(string subdir, string filename, string text)
    {
        var path = Path.Combine(directory, subdir, filename);

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
        var path = Path.Combine(directory, subdir, filename.ToPlatformPath());
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

        var file = new FileInfo(path);
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
        var path = Path.Combine(directory, subdir, filename);
        return File.Exists(path);
    }

    /// <summary>
    /// Returns true if it's either an empty directory or it contains CanvasManifest.json file.
    /// </summary>
    /// <returns></returns>
    private bool ValidateSafeToDelete(ErrorContainer errors)
    {
        if (Directory.EnumerateFiles(directory).Any() && !File.Exists(Path.Combine(directory, "CanvasManifest.json")))
        {
            errors.BadParameter("Must provide path to either empty directory or a directory where the app was previously unpacked.");
            throw new DocumentException();
        }
        return true;
    }
}
