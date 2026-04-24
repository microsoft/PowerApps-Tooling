// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Persistence.Tests;

public static class TestingUtilities
{
    /// <summary>
    /// Gets the file names for all files directly in the specified directory (TopDirectoryOnly).
    /// </summary>
    public static string[] GetFileNamesInDirectory(string folderPath)
    {
        return [.. Directory.GetFiles(folderPath)
            .Select(static p => Path.GetFileName(p))
            .Order(FilePathComparer.Instance)];
    }

    /// <summary>
    /// Gets the names of the top-level directories in the specified directory.
    /// </summary>
    public static string[] GetSubDirectoryNamesInDirectory(string folderPath)
    {
        return [.. Directory.GetDirectories(folderPath)
            .Select(static p => Path.GetFileName(p))
            .Order(FilePathComparer.Instance)];
    }

    /// <summary>
    /// Gets the file paths for all files under the specified directory (recursive by default).
    /// All paths are relative to the input path and normalized to use the back-slash character ('\') as the path separator (allowing easier xplat comparisons).
    /// </summary>
    public static string[] GetNormalizedFilePathsUnderDirectory(string folderPath)
    {
        return [.. Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
            .Select(p => p[(folderPath.Length + 1)..].Replace('/', '\\')) // trim the testDir from the beginning of the paths.
            .Order(FilePathComparer.Instance)];
    }

    /// <summary>
    /// Gets the file paths for all files under the specified directory (recursive by default).
    /// All paths are relative to the input path and normalized to use the back-slash character ('\') as the path separator (allowing easier xplat comparisons).
    /// </summary>
    public static string[] GetNormalizedDirectoryPathsUnderDirectory(string folderPath)
    {
        return [.. Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories)
            .Select(p => p[(folderPath.Length + 1)..].Replace('/', '\\')) // trim the testDir from the beginning of the paths.
            .Order(FilePathComparer.Instance)];
    }

    /// <summary>
    /// Copies the contents of the <paramref name="sourceFolderPath"/> to the <paramref name="destinationFolderPath"/>.
    /// </summary>
    /// <param name="overwrite">true to overwrite existing files. Otherwise, existing files will cause an <see cref="IOException"/>.</param>
    public static void CopyFolderRecursively(string sourceFolderPath, string destinationFolderPath, bool overwrite = false)
    {
        var sourceDir = new DirectoryInfo(sourceFolderPath);
        if (!sourceDir.Exists)
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceFolderPath);
        }

        // If the destination directory doesn't exist, create it.
        Directory.CreateDirectory(destinationFolderPath);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = sourceDir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destinationFolderPath, file.Name);
            file.CopyTo(tempPath, overwrite);
        }

        // Get the subdirectories for the specified directory.
        foreach (DirectoryInfo subdir in sourceDir.GetDirectories())
        {
            CopyFolderRecursively(subdir.FullName, Path.Combine(destinationFolderPath, subdir.Name), overwrite);
        }
    }

    public static int? FindIndexOfFirstNotEqual(string[] baselineLines, string[] actualLines)
    {
        ThrowIfNull(baselineLines);
        ThrowIfNull(actualLines);

        for (int i = 0; i < baselineLines.Length; i++)
        {
            if (i >= actualLines.Length)
            {
                return i;
            }

            if (actualLines[i] != baselineLines[i])
            {
                return i;
            }
        }

        if (actualLines.Length > baselineLines.Length)
        {
            return baselineLines.Length;
        }

        return null;
    }


}
