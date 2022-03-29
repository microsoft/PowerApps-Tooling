// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
                if(Directory.Exists(entropyPath))
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
       
        private static T ToObject<T>(ZipArchiveEntry entry)
        {
            var je = entry.ToJson();
            return je.ToObject<T>();
        }
    }
}
