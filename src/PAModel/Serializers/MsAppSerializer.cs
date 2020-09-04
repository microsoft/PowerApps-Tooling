// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using PAModel.Serializers;

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

            ComponentsMetadataJson dcmetadata = null;
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

                        case FileKind.OldEntityJSon:
                            throw new NotSupportedException($"This is using an older msapp format that is not supported.");

                        case FileKind.DataComponentTemplates:
                            dctemplate = ToObject<DataComponentTemplatesJson>(entry);
                            break;
                        case FileKind.ComponentsMetadata:
                            dcmetadata = ToObject<ComponentsMetadataJson>(entry);
                            break;
                        case FileKind.DataComponentSources:
                            dcsources = ToObject<DataComponentSourcesJson>(entry);
                            break;

                        case FileKind.Properties:
                            app._properties = ToObject<DocumentPropertiesJson>(entry);
                            break;

                        case FileKind.Header:
                            app._header = ToObject<HeaderJson>(entry);
                            app._entropy.SetHeaderLastSaved(app._header.LastSavedDateTimeUTC);
                            app._header.LastSavedDateTimeUTC = null;
                            break;

                        case FileKind.PublishInfo:
                            app._publishInfo = ToObject<PublishInfoJson>(entry);
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

                                int iOrder = 0;
                                foreach (var ds in dataSources.DataSources)
                                {
                                    app.AddDataSourceForLoad(ds, iOrder);
                                    iOrder++;
                                }
                            }
                            break;
                    }
                }

                // Normalize logo filename. 
                app.TranformLogoOnLoad();

                if (dcmetadata?.Components != null)
                {
                    int order = 0;
                    foreach (var x in dcmetadata.Components)
                    {
                        var dc = MinDataComponentManifest.Create(x);
                        app._dataComponents.Add(x.TemplateName, dc); // should be unique.

                        app._entropy.Add(x, order);
                        order++;
                    }
                }

                // Only for data-compoents. 
                if (dctemplate?.ComponentTemplates != null)
                {
                    int order = 0;
                    foreach (var x in dctemplate.ComponentTemplates)
                    {
                        MinDataComponentManifest dc = app._dataComponents[x.Name]; // Should already exist
                        app._entropy.SetTemplateVersion(x.Name, x.Version);
                        app._entropy.Add(x, order);
                        dc.Apply(x);
                        order++;
                    }
                }

                if (dcsources?.DataSources != null)
                {
                    // Component Data sources only appear if the data component is actually 
                    // used as a data source in this app. 
                    foreach (var x in dcsources.DataSources)
                    {
                        if (x.Type != DataComponentSourcesJson.NativeCDSDataSourceInfo)
                        {
                            throw new NotImplementedException(x.Type);
                        }
                        
                        var ds = new DataSourceEntry
                        {
                             Name = x.Name,
                             DataComponentDetails = x, // pass in all details for full-fidelity
                             Type = DataSourceModel.DataComponentType
                        };

                        app.AddDataSourceForLoad(ds);
                    }
                }
            }
                                    
            app.OnLoadComplete();

            // app.TransformTemplatesOnLoad(); 

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
                // $$$ allow zips
                // throw new InvalidOperationException("Only works for .msapp files");
            }

            if (File.Exists(fullpathToMsApp)) // Overwrite!
            {
                File.Delete(fullpathToMsApp);
            }
            using (var z = ZipFile.Open(fullpathToMsApp, ZipArchiveMode.Create))
            {
                foreach (FileEntry entry in app.GetMsAppFiles())
                {
                    if (entry != null)
                    {
                        var e = z.CreateEntry(entry.Name);
                        using (var dest = e.Open())
                        {
                            dest.Write(entry.RawBytes, 0, entry.RawBytes.Length);
                        }
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


            var header = app._header.JsonClone();
            header.LastSavedDateTimeUTC = app._entropy.GetHeaderLastSaved();
            yield return ToFile(FileKind.Header, header);

            yield return ToFile(FileKind.Properties, app._properties);

            var (publishInfo, logoFile) = app.TransformLogoOnSave();
            yield return logoFile;
            yield return ToFile(FileKind.PublishInfo, publishInfo);

            // "DataComponent" data sources are not part of DataSource.json, and instead in their own file
            var dataSources = new DataSourcesJson
            {
                DataSources = app.GetDataSources()
                    .Where(x => !x.IsDataComponent)
                    .OrderBy(x => app._entropy.GetOrder(x))
                    .ToArray()
            };
            yield return ToFile(FileKind.DataSources, dataSources);            

            // Rehydrate sources that used a data component. 

            foreach (var sourceFile in app._sources.Values)
            {
                var sf = sourceFile;
                
                // sf = app.RehydrateOnSave(sf);                

                yield return sf.ToMsAppFile();
            }
            
            var dcmetadataList = new List< ComponentsMetadataJson.Entry>();
            var dctemplate = new List<TemplateMetadataJson>();

            foreach (MinDataComponentManifest dc in app._dataComponents.Values)
            {
                dcmetadataList.Add(new ComponentsMetadataJson.Entry
                {
                    Name = dc.Name,
                    TemplateName = dc.TemplateGuid,
                    //Description = dc.Description,
                    //AllowCustomization = true,
                    ExtensionData = dc.ExtensionData
                });

                if (dc.IsDataComponent)
                {
                    // Need to looup ControlUniqueId. 
                    var controlId = app.LookupControlIdsByTemplateName(dc.TemplateGuid).First();


                    var template = new TemplateMetadataJson
                    {
                        Name = dc.TemplateGuid,
                        Version = app._entropy.GetTemplateVersion(dc.TemplateGuid),
                        IsComponentLocked = false,
                        ComponentChangedSinceFileImport = true,
                        ComponentAllowCustomization = true,
                        CustomProperties = dc.CustomProperties,
                        DataComponentDefinitionKey = dc.DataComponentDefinitionKey
                    };

                    // Rehydrate fields. 
                    template.DataComponentDefinitionKey.ControlUniqueId = controlId;

                    dctemplate.Add(template);                    
                }
            }

            if (dcmetadataList.Count > 0)
            {
                // If the components file is present, then write out all files. 
                yield return ToFile(FileKind.ComponentsMetadata, new ComponentsMetadataJson
                {
                    Components = dcmetadataList
                            .OrderBy(x => app._entropy.GetOrder(x))
                            .ToArray()
                });
            }

            if (dctemplate.Count > 0)
            { 
                yield return ToFile(FileKind.DataComponentTemplates, new DataComponentTemplatesJson
                {
                     ComponentTemplates = dctemplate
                        .OrderBy(x => app._entropy.GetOrder(x))
                        .ToArray()
                });
            }


            // Rehydrate the DataComponent DataSource file. 
            {              
                IEnumerable<DataComponentSourcesJson.Entry> ds =
                   from item in app.GetDataSources().Where(x => x.IsDataComponent)
                   select item.DataComponentDetails;

                var dsArray = ds.ToArray();
                
                // backcompat-nit: if we have any DC, then always emit the DC Sources file, even if empty.
                // if (dcmetadataList.Count > 0)
                if (dctemplate.Count > 0 || dsArray.Length > 0)
                {
                    yield return ToFile(FileKind.DataComponentSources, new DataComponentSourcesJson
                    {
                        DataSources = dsArray
                    });
                }
            }
        }


        internal static FileEntry ToFile<T>(FileKind kind, T value)
        {
            var filename = FileEntry.GetFilenameForKind(kind);

            var jsonStr = JsonSerializer.Serialize(value, Utility._jsonOpts);

            jsonStr = JsonNormalizer.Normalize(jsonStr);

            var bytes = Encoding.UTF8.GetBytes(jsonStr);

            return new FileEntry { Name = filename, RawBytes = bytes };
        }
    }



}