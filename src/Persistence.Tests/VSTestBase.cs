// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace Persistence.Tests;

/// <summary>
/// Represents a shared base class for any test class that uses the Visual Studio Test Framework.
/// </summary>
/// <remarks>
/// DO NOT add any setup/tear down logic to this class, as not all tests may require it.
/// The preferred approach is to use a different derived base class for tests that require setup/tear down logic specific to some shared scenarios.
/// </remarks>
public abstract class VSTestBase
{
    public required TestContext TestContext { get; init; }

    /// <summary>
    /// The path to the root output folder for all test folders.
    /// Default is `"testout"`; which makes it relative to the test assembly's location.
    /// It is expected that this root folder is a folder shared amongst all tests. Unit tests should only ever add content to sub-folders of this folder in order to ensure deterministic test runs.
    /// </summary>
    public string TestsOutputFolderRoot { get; protected init; } = "testout";

    /// <summary>
    /// Creates an output folder and returns its path.
    /// </summary>
    /// <param name="testCaseName">The name of a test case. This is used as a sub-folder name.</param>
    /// <param name="testName">The name of the test. This parameter should not be specified, let the compiler provide it.</param>
    /// <param name="ensureEmpty">If true, will delete the existing directory. Helpful for tests that generate a lot of files when rerunning tests locally.</param>
    /// <returns>The path to the folder that was created.</returns>
    protected string CreateTestCaseOutputFolder(string testCaseName, [CallerMemberName] string? testName = null, bool ensureEmpty = false)
    {
        ThrowIfNullOrEmpty(testCaseName);
        _ = testName ?? throw new ArgumentNullException(nameof(testName), "This argument should be specified by the compiler, or else pass the value in explicitly.");

        string testOutputFolderPath = Path.Combine(CreateTestOutputFolder(testName, ensureEmpty: false), testCaseName);
        if (ensureEmpty)
        {
            DeleteDirectoryRecursivelyWithRetries(testOutputFolderPath);
        }

        Directory.CreateDirectory(testOutputFolderPath);

        return testOutputFolderPath;
    }

    /// <summary>
    /// Creates an output folder and returns its path.
    /// </summary>
    /// <param name="testName">The name of the test. This parameter should not be specified, let the compiler provide it.</param>
    /// <param name="ensureEmpty">If true, will delete the existing directory. Helpful for tests that generate a lot of files when rerunning tests locally.</param>
    /// <returns>The path to the folder that was created.</returns>
    protected string CreateTestOutputFolder([CallerMemberName] string? testName = null, bool ensureEmpty = false)
    {
        _ = testName ?? throw new ArgumentNullException(nameof(testName), "This argument should be specified by the compiler, or else pass the value in explicitly.");

        string testClassOutputFolder = Path.GetFullPath(Path.Combine(TestsOutputFolderRoot, GetType().Name));
        string testOutputFolderPath = Path.Combine(testClassOutputFolder, testName);
        if (ensureEmpty)
        {
            DeleteDirectoryRecursivelyWithRetries(testOutputFolderPath);
        }

        Directory.CreateDirectory(testOutputFolderPath);

        return testOutputFolderPath;
    }

    protected void DeleteDirectoryRecursivelyWithRetries(string folderPath)
    {
        var fullPath = Path.GetFullPath(folderPath);
#if NET462_OR_GREATER
        // In order to support deletion of files with long filenames in NetFx, we use the 'extended-length path prefix':
        fullPath = $@"\\?\{fullPath}";
#endif

        // When rerunning some tests, the existing Directory.Delete may throw UnauthorizedAccessException if there were any symbolic links.
        // e.g. Pcf projects that use location to local packages, npm creates symbolic links to the local folder, rather than copying.
        const int retries = 3;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(fullPath, recursive: true);
                }

                // End if successful
                return;
            }
            catch (Exception ex) when (i < retries - 1) // only catch if we'll be doing a retry
            {
                // swallow, and try again
                TestContext.WriteLine($"Warning: Retrying deletion of folder '{fullPath}' due to {ex.GetType()} exception. The message was: {ex.Message}");
                Thread.Sleep(5000);
            }
        }
    }
}
