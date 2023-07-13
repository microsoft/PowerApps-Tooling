// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace PAModelTests
{
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
                string outSrcDir = tempDir.Dir;

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

                    using (var stream = new FileStream(tempFile.FullPath, FileMode.Open))
                    {
                        // Read the msapp file
                        ZipArchive zipOpen = new ZipArchive(stream, ZipArchiveMode.Read);

                        foreach (var entry in zipOpen.Entries)
                        {
                            var kind = FileEntry.TriageKind(FilePath.FromMsAppPath(entry.FullName));

                            switch (kind)
                            {
                                // Validate that the last entry in the DataSources.json is TableDefinition entry.
                                case FileKind.DataSources:
                                    {
                                        var dataSourcesFromMsapp = ToObject<DataSourcesJson>(entry);
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
            ErrorContainer errorsCaptured = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
            errorsCaptured.ThrowOnErrors();
        }

        [DataTestMethod]
        [DataRow("UnusedDataSourcesHashMismatch.msapp")]
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

            Assert.IsFalse(msApp._entropy.WasUnusedDataSourcesForLocalDbRefsAbsent());
            Assert.IsTrue(msApp._entropy.GetUnusedDataSourcesForLocalDbRef("default.cds").Count > 0);
        }

        [DataTestMethod]
        [DataRow("UnusedDataSourcesHashMismatch.msapp")]
        public void TestUnusedDataSourcesAreNotPreservedWhenEntropyDoesNotTrackThemBackwardCompat(string appName)
        {
            var pathToMsApp = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(pathToMsApp));

            var (msApp, errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
            errors.ThrowOnErrors();

            using var sourcesTempDir = new TempDir();
            var sourcesTempDirPath = sourcesTempDir.Dir;
            errors = msApp.SaveToSources(sourcesTempDirPath, pathToMsApp);
            errors.ThrowOnErrors();
            msApp._dataSourceReferences["default.cds"].dataSources.Remove(msApp._entropy.GetUnusedDataSourcesForLocalDbRef("default.cds").Keys.First());
            using var sourcesTempDir2 = new TempDir();
            errors = msApp.SaveToSources(sourcesTempDir2.Dir, pathToMsApp);

            Assert.IsTrue(errors.HasErrors || errors.HasWarnings);
        }

        [DataTestMethod]
        [DataRow("UnusedDataSourcesHashMismatch.msapp")]
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

            Assert.IsFalse(msApp._entropy.WasUnusedDataSourcesForLocalDbRefsAbsent());
            Assert.IsNull(msApp._entropy.GetUnusedDataSourcesForLocalDbRef("default.cds"));
            Assert.IsTrue(msApp._entropy.UnusedDataSourcesForLocalDbRefs.ContainsKey("default.cds"));
        }

        [DataTestMethod]
        [DataRow("UnusedDataSourcesHashMismatch.msapp")]
        public void TestWhenDataSourcesIsSetEmptyDictionary(string appName)
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

            Assert.IsFalse(msApp._entropy.WasUnusedDataSourcesForLocalDbRefsAbsent());
            Assert.IsTrue(msApp._entropy.GetUnusedDataSourcesForLocalDbRef("default.cds").Count == 0);
        }

        [DataTestMethod]
        [DataRow("UnusedDataSourcesHashMismatch.msapp")]
        public void TestWhenDataSourcesAreNotPresentBackwardCompat(string appName)
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
            var entropy = ReadEntropy(sources2.Dir);
            entropy.UnusedDataSourcesForLocalDbRefs = null;
            WriteEntropy(sources2.Dir, entropy);
            (msApp, errors) = CanvasDocument.LoadFromSources(sources2.Dir);
            errors.ThrowOnErrors();

            Assert.IsTrue(msApp._entropy.WasUnusedDataSourcesForLocalDbRefsAbsent());
            Assert.IsNull(msApp._entropy.GetUnusedDataSourcesForLocalDbRef("default.cds"));
        }

        private static Entropy ReadEntropy(string sources)
        {
            var pathToEntropy = Path.Combine(sources, "Entropy", "Entropy.Json");
            return JsonSerializer.Deserialize<Entropy>(File.ReadAllText(pathToEntropy), Utilities._jsonOpts); 
        }

        private static void WriteEntropy(string sources, Entropy entropy)
        {
            var pathToEntropy = Path.Combine(sources, "Entropy", "Entropy.Json");
            File.WriteAllText(pathToEntropy, JsonSerializer.Serialize(entropy, Utilities._jsonOpts));
        }

        [DataTestMethod]
        [DataRow(new string[] {"FileNameOne.txt" }, ".txt")]
        [DataRow(new string[] {"FileNameTwo.tx<t" }, ".tx%3ct")]
        [DataRow(new string[] { "FileNameThr<ee.txt" }, ".txt")]
        public void TestGetExtensionEncoded(string[] fileExtension, string encodedExtension)
        {
            FilePath filePath = new FilePath(fileExtension);
            Assert.AreEqual(filePath.GetExtension(), encodedExtension);
        }

        private static T ToObject<T>(ZipArchiveEntry entry)
        {
            var je = entry.ToJson();
            return je.ToObject<T>();
        }
    }
}
