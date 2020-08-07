using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text.Json;
using System.Linq;
using System.Text;

namespace PAModel
{
    // Read/Write to an .msapp file. 
    public static class MsAppSerializer
    {
        private static T ToObject<T>(ZipArchiveEntry entry)
        {
            var je = entry.ToJson();
            return je.ToObject<T>();
        }
        public static MsApp Load(string fullpathToMsApp)
        {
            if (!fullpathToMsApp.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only works for .msapp files");
            }

            // Read raw files. 
            // Apply transforms. 
            var app = new MsApp();

            DataComponentsMetadataJson dcmetadata = null;
            DataComponentTemplatesJson dctemplate = null;
            DataComponentSourcesJson dcsources  = null;

            using (var z = ZipFile.OpenRead(fullpathToMsApp))
            {
                foreach (var entry in z.Entries)
                {
                    var fullName = entry.FullName;
                    var kind = FileEntry.TriageKind(fullName);

                    switch (kind)
                    {
                        default:
                            // Track any unrecognized files so we can save back.
                            app.AddFile(FileEntry.FromZip(entry));
                            break;

                        case FileKind.DataComponentTemplates:
                            dctemplate = ToObject<DataComponentTemplatesJson>(entry);
                            break;
                        case FileKind.ComponentsMetadata:
                            dcmetadata = ToObject<DataComponentsMetadataJson>(entry);
                            break;
                        case FileKind.DataComponentSources:
                            dcsources = ToObject<DataComponentSourcesJson>(entry);
                            break;

                        case FileKind.Properties:
                            app._properties = ToObject<DocumentPropertiesJson>(entry);
                            break;

                        case FileKind.Header:
                            app._header = ToObject<HeaderJson>(entry);
                            break;

                        case FileKind.ComponentSrc:
                        case FileKind.ControlSrc:
                            {
                                var control = ToObject<ControlInfoJson>(entry);
                                var sf = SourceFile.New(control);
                                app._sources.Add(sf.ControlName, sf);
                            }
                            break;



                        case FileKind.DataSources:
                            {
                                var dataSources = ToObject<DataSourcesJson>(entry);
                                Utility.EnsureNoExtraData(dataSources.ExtensionData);

                                foreach (var ds in dataSources.DataSources)
                                {
                                    app._dataSources[ds.Name] = ds;
                                }
                            }
                            break;
                    }
                }


                if (dcmetadata?.Components != null)
                {
                    foreach (var x in dcmetadata.Components)
                    {
                        var dc = app._dataComponents.GetOrCreate(x.TemplateName);
                        dc._metadata = x;
                    }
                }
                if (dctemplate?.ComponentTemplates != null)
                {
                    foreach (var x in dctemplate.ComponentTemplates)
                    {
                        var dc = app._dataComponents.GetOrCreate(x.Name);
                        dc._template = x;
                    }
                }
                if (dcsources?.DataSources != null)
                {
                    foreach (var x in dcsources.DataSources)
                    {
                        var dc = app._dataComponents.GetOrCreate(x.AssociatedDataComponentTemplate);
                        dc._dcsources = x;
                    }
                }
            }

            app.OnLoadComplete();

            return app;
        }

        internal static void AddFile(this MsApp app, FileEntry entry)
        {
            app._unknownFiles.Add(entry.Name, entry);
        }


        // Write back out to a msapp file. 
        public static void SaveAsMsApp(this MsApp app, string fullpathToMsApp)
        {
            if (!fullpathToMsApp.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only works for .msapp files");
            }

            if (File.Exists(fullpathToMsApp)) // Overwrite!
            {
                File.Delete(fullpathToMsApp);
            }
            using (var z = ZipFile.Open(fullpathToMsApp, ZipArchiveMode.Create))
            {
                foreach (FileEntry entry in app.GetMsAppFiles())
                {
                    var e = z.CreateEntry(entry.Name);
                    using (var dest = e.Open())
                    {
                        dest.Write(entry.RawBytes, 0, entry.RawBytes.Length);
                    }
                }
            }
        }

        // Get everything that should be stored as a file in the .msapp.
        private static IEnumerable<FileEntry> GetMsAppFiles(this MsApp app)
        {
            // Loose files
            foreach (var file in app._unknownFiles.Values)
            {
                yield return file;
            }

            yield return ToFile(FileKind.Header, app._header);
            yield return ToFile(FileKind.Properties, app._properties);

            var dataSources = new DataSourcesJson
            {
                DataSources = app._dataSources.Values.ToArray()
            };
            yield return ToFile(FileKind.DataSources, dataSources);

            foreach (var sourceFile in app._sources.Values)
            {
                yield return sourceFile.ToMsAppFile();
            }

            var dcmetadataList = new List< DataComponentsMetadataJson.Entry>();
            var dctemplate = new List<DataComponentTemplatesJson.Entry>();
            var dcsources = new List<DataComponentSourcesJson.Entry>();

            foreach(var dataComponent in app._dataComponents.Values)
            {
                if (dataComponent._metadata != null)
                {
                    dcmetadataList.Add(dataComponent._metadata);
                }
                if (dataComponent._template != null)
                {
                    dctemplate.Add(dataComponent._template);
                }
                if (dataComponent._dcsources != null)
                {
                    dcsources.Add(dataComponent._dcsources);
                }
            }
            if (dcmetadataList.Count > 0)
            {
                // If the components file is present, then write out all files. 
                yield return ToFile(FileKind.ComponentsMetadata, new DataComponentsMetadataJson
                {
                     Components = dcmetadataList.ToArray()
                });
            
                yield return ToFile(FileKind.DataComponentTemplates, new DataComponentTemplatesJson
                {
                     ComponentTemplates = dctemplate.ToArray()
                });
            
                yield return ToFile(FileKind.DataComponentSources, new DataComponentSourcesJson
                {
                    DataSources = dcsources.ToArray()
                });
            }
        }

        internal static FileEntry ToFile<T>(FileKind kind, T value)
        {
            var filename = FileEntry.GetFilenameForKind(kind);

            var jsonStr = JsonSerializer.Serialize(value, Utility._jsonOpts);
            var bytes = Encoding.UTF8.GetBytes(jsonStr);

            return new FileEntry { Name = filename, RawBytes = bytes };
        }
    }



}