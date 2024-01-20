// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.MsApp;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;

namespace PAModelTests;

// DataSources Tests
[TestClass]
public class DataSourceTests
{
    // Validates that the TableDefinitions are being added at the end of the DataSources.json when the entropy file is deleted.
    [DataTestMethod]
    [DataRow("GalleryTestApp.msapp")]
    [DataRow("AccountPlanReviewerMaster.msapp")]
    public void TestTableDefinitionsAreLastEntriesWhenEntropyDeleted(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        using (var tempDir = new TempDir())
        {
            var outSrcDir = tempDir.Dir;

            // Save to sources
            msapp.SaveToSources(outSrcDir);

            // Delete Entropy directory
            var entropyPath = Path.Combine(outSrcDir, "Entropy");
            if (Directory.Exists(entropyPath))
            {
                Directory.Delete(entropyPath, true);
            }

            // Load app from the sources after deleting the entropy
            var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

            using (var tempFile = new TempFile())
            {
                // Repack the app
                MsAppSerializer.SaveAsMsApp(app, tempFile.FullPath, new ErrorContainer());

                using (var msappArchive = new MsappArchive(tempFile.FullPath))
                {
                    foreach (var entry in msappArchive.CanonicalEntries)
                    {
                        var kind = FileEntry.TriageKind(FilePath.FromMsAppPath(entry.Value.FullName));

                        switch (kind)
                        {
                            // Validate that the last entry in the DataSources.json is TableDefinition entry.
                            case FileKind.DataSources:
                                {
                                    var dataSourcesFromMsapp = ToObject<DataSourcesJson>(entry.Value);
                                    var last = dataSourcesFromMsapp.DataSources.LastOrDefault();
                                    Assert.AreEqual(last.TableDefinition != null, true);
                                    return;
                                }
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }

    [DataTestMethod]
    [DataRow("EmptyLocalDBRefsHashMismatchProperties.msapp")]
    public void TestNoLocalDatabaseRefsWhenLocalDatabaseReferencesPropertyWasEmptyJson(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        msApp.SaveToSources(sourcesTempDirPath);

        var loadedMsApp = SourceSerializer.LoadFromSource(sourcesTempDirPath, new ErrorContainer());
        Assert.IsTrue(loadedMsApp._entropy.WasLocalDatabaseReferencesEmpty.Value);
        Assert.IsFalse(loadedMsApp._entropy.LocalDatabaseReferencesAsEmpty);
        Assert.IsTrue(loadedMsApp._dataSourceReferences.Count == 0);
    }

    [DataTestMethod]
    [DataRow("EmptyLocalDBRefsHashMismatchProperties.msapp")]
    public void TestConnectionInstanceIDHandling(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        // Testing if conn instance id is added to entropy
        Assert.IsNotNull(msApp._entropy.LocalConnectionIDReferences);

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        var errorsCaptured = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
        errorsCaptured.ThrowOnErrors();
    }

    [DataTestMethod]
    [DataRow("MultipleDataSourcesWithOneUnused.msapp")]
    public void TestUnusedDataSourcesArePreserved(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        errors = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
        errors.ThrowOnErrors();

        var (msApp1, errors1) = CanvasDocument.LoadFromSources(sourcesTempDirPath);

        Assert.AreEqual(msApp._dataSourceReferences.First().Key, msApp._dataSourceReferences.First().Key);
        var actualDataSources = msApp1._dataSourceReferences.First().Value.dataSources;
        var expectedDataSources = msApp._dataSourceReferences.First().Value.dataSources;
        Assert.AreEqual(expectedDataSources.Count, actualDataSources.Count);
        Assert.IsTrue(actualDataSources.ContainsKey("environment_39a902ba"));
        foreach (var kvp in actualDataSources)
        {
            Assert.IsTrue(expectedDataSources.ContainsKey(kvp.Key));
            var expectedDataSource = expectedDataSources[kvp.Key];
            var actualDataSource = kvp.Value;
            Assert.AreEqual(expectedDataSource.ExtensionData.Count, actualDataSource.ExtensionData.Count);
            foreach (var kvpExtension in actualDataSource.ExtensionData)
            {
                Assert.IsTrue(expectedDataSource.ExtensionData.ContainsKey(kvpExtension.Key));
            }
        }
    }

    [DataTestMethod]
    [DataRow("MultipleDataSourcesWithOneUnused.msapp")]
    public void TestUnusedDataSourcesAreNotPreservedWhenNotTracked(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        var expectedDataSources = msApp._dataSourceReferences.First().Value.dataSources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        msApp._dataSourceReferences.First().Value.dataSources.Remove("environment_39a902ba");
        errors.ThrowOnErrors();

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        errors = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
        Assert.IsTrue(errors.HasErrors);

        var (msApp1, errors1) = CanvasDocument.LoadFromSources(sourcesTempDirPath);
        errors1.ThrowOnErrors();

        var actualDataSources = msApp1._dataSourceReferences.First().Value.dataSources;
        Assert.AreEqual(expectedDataSources.Count - actualDataSources.Count, 1);
        foreach (var key in expectedDataSources.Keys)
        {
            if (key == "environment_39a902ba")
            {
                Assert.IsFalse(actualDataSources.ContainsKey(key));
            }
            else
            {
                Assert.IsTrue(actualDataSources.ContainsKey(key));
            }
        }
    }

    [DataTestMethod]
    [DataRow("MultipleDataSourcesWithOneUnused.msapp")]
    public void TestWhenDataSourcesAreNotPresent(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        msApp._dataSourceReferences["default.cds"].dataSources = null;
        errors.ThrowOnErrors();

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        errors = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
        (msApp, errors) = CanvasDocument.LoadFromSources(sourcesTempDirPath);
        using var msAppTemp = new TempFile();
        using var sources2 = new TempDir();
        var errors2 = new ErrorContainer();
        MsAppSerializer.SaveAsMsApp(msApp, msAppTemp.FullPath, errors2);
        errors = msApp.SaveToSources(sources2.Dir, msAppTemp.FullPath);
        errors.ThrowOnErrors();

        Assert.IsTrue(msApp._dataSourceReferences.ContainsKey("default.cds"));
        Assert.IsNull(msApp._dataSourceReferences.First().Value.dataSources);
    }

    [DataTestMethod]
    [DataRow("MultipleDataSourcesWithOneUnused.msapp")]
    public void TestWhenDataSourcesIsSetToEmptyDictionary(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        msApp._dataSourceReferences["default.cds"].dataSources = new Dictionary<string, LocalDatabaseReferenceDataSource>();
        errors.ThrowOnErrors();

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        errors = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
        (msApp, errors) = CanvasDocument.LoadFromSources(sourcesTempDirPath);
        using var msAppTemp = new TempFile();
        using var sources2 = new TempDir();
        var errors2 = new ErrorContainer();
        MsAppSerializer.SaveAsMsApp(msApp, msAppTemp.FullPath, errors2);
        errors = msApp.SaveToSources(sources2.Dir, msAppTemp.FullPath);
        errors.ThrowOnErrors();

        Assert.AreEqual(msApp._dataSourceReferences.First().Value.dataSources.Count, 0);
    }

    [DataTestMethod]
    [DataRow("NoUnusedDataSources.msapp")]
    public void TestAllUsedDataSourcesArePreserved(string appName)
    {
        var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(pathToMsApp));

        var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        using var sourcesDir = new TempDir();
        errors = msApp.SaveToSources(sourcesDir.Dir);
        errors.ThrowOnErrors();

        var (msApp1, errors1) = CanvasDocument.LoadFromSources(sourcesDir.Dir);
        errors1.ThrowOnErrors();

        Assert.AreEqual(msApp._dataSourceReferences["default.cds"].dataSources.Count, msApp._dataSourceReferences["default.cds"].dataSources.Count);
        foreach (var entry in msApp._dataSourceReferences["default.cds"].dataSources.Keys.OrderBy(key => key).Zip(msApp1._dataSourceReferences["default.cds"].dataSources.Keys.OrderBy(key => key)))
        {
            Assert.AreEqual(entry.First, entry.Second);
        }
    }

    [DataTestMethod]
    [DataRow(new string[] { "FileNameOne.txt" }, ".txt")]
    [DataRow(new string[] { "FileNameTwo.tx<t" }, ".tx%3ct")]
    [DataRow(new string[] { "FileNameThr<ee.txt" }, ".txt")]
    public void TestGetExtensionEncoded(string[] fileExtension, string encodedExtension)
    {
        var filePath = new FilePath(fileExtension);
        Assert.AreEqual(filePath.GetExtension(), encodedExtension);
    }

    private static T ToObject<T>(ZipArchiveEntry entry)
    {
        var je = entry.ToJson();
        return je.ToObject<T>();
    }
}
