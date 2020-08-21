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

                                foreach (var ds in dataSources.DataSources)
                                {
                                    app.AddDataSourceForLoad(ds);
                                }
                            }
                            break;
                    }
                }

                // Normalize logo filename. 
                app.NormalizeLogoFile();

                if (dcmetadata?.Components != null)
                {
                    foreach (var x in dcmetadata.Components)
                    {
                        var dc = app._dataComponents.GetOrCreate(x.TemplateName);                        
                        dc.Apply(x);
                    }
                }

                if (dctemplate?.ComponentTemplates != null)
                {
                    foreach (var x in dctemplate.ComponentTemplates)
                    {
                        MinDataComponentManifest dc = app._dataComponents[x.Name]; // Should already exist
                        app._entropy.SetTemplateVersion(x.Name, x.Version);
                        dc.Apply(x);
                    }
                }

                if (dcsources?.DataSources != null)
                {
                    foreach (var x in dcsources.DataSources)
                    {
                        if (x.Type != DataComponentSourcesJson.NativeCDSDataSourceInfo)
                        {
                            throw new NotImplementedException(x.Type);
                        }
                        
                        // var dc = app._dataComponents.GetOrCreate(x.AssociatedDataComponentTemplate);
                        // dc._dcsources = x;
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

            return app;
        }

        internal static void AddFile(this MsApp app, FileEntry entry)
        {
            app._unknownFiles.Add(entry.Name, entry);
        }

        // Logo file has a random filename that is continually regenerated, which creates Noisy Diffs.        
        // Find the file - based on the PublishInfo.LogoFileName and pull it out. 
        // Normalize name (logo.jpg), touchup PublishInfo so that it's stable.
        // Save the old name in Entropy so that we can still roundtrip. 
        private static void NormalizeLogoFile(this MsApp app)
        {
            // May be null or "" 
            var oldLogoName = app._publishInfo.LogoFileName;
            if (!string.IsNullOrEmpty(oldLogoName))
            {
                string newLogoName = "logo" + Path.GetExtension(oldLogoName);
                
                FileEntry logoFile;
                var oldKey = @"Resources\" + oldLogoName;
                if (app._unknownFiles.TryGetValue(oldKey, out logoFile))
                {
                    app._unknownFiles.Remove(oldKey);

                    logoFile.Name = @"Resources\" + newLogoName;
                    app._logoFile = logoFile;


                    app._entropy.SetLogoFileName(oldLogoName);
                    app._publishInfo.LogoFileName = newLogoName;
                }
            }
        }

        // Get the original logo file (using entropy to get the old name) 
        // And return a touched publishInfo pointing to it.
        private static (PublishInfoJson, FileEntry) DenormalizeLogoFile(this MsApp app)
        {
            FileEntry logoFile = null;
            var publishInfo = app._publishInfo.JsonClone();

            if (!string.IsNullOrEmpty(publishInfo.LogoFileName))
            {
                publishInfo.LogoFileName = app._entropy.OldLogoFileName ?? Path.GetFileName(app._logoFile.Name);
                logoFile = new FileEntry
                {
                    Name = @"Resources\" + publishInfo.LogoFileName,
                    RawBytes = app._logoFile.RawBytes
                };
            }

            return (publishInfo, logoFile);
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

            var (publishInfo, logoFile) = app.DenormalizeLogoFile();
            yield return logoFile;
            yield return ToFile(FileKind.PublishInfo, publishInfo);

            // "DataComponent" data sources are not part of DataSource.json, and instead in their own file
            var dataSources = new DataSourcesJson
            {
                DataSources = app.GetDataSources().Where(x => !x.IsDataComponent).ToArray()
            };
            yield return ToFile(FileKind.DataSources, dataSources);


            foreach (var sourceFile in app._sources.Values)
            {
                yield return sourceFile.ToMsAppFile();
            }
            
            var dcmetadataList = new List< DataComponentsMetadataJson.Entry>();
            var dctemplate = new List<TemplateMetadataJson>();

            foreach (MinDataComponentManifest dc in app._dataComponents.Values)
            {
                dcmetadataList.Add(new DataComponentsMetadataJson.Entry
                {
                    Name = dc.Name,
                    TemplateName = dc.TemplateGuid,
                    Description = dc.Description,
                    AllowCustomization = true,
                });

                // Need to looup ControlUniqueId. 
                var controlId = app.LookupControlIdsByTemplateName(dc.TemplateGuid).First();

                var template = new TemplateMetadataJson
                {
                    Name = dc.TemplateGuid,
                    Version = app._entropy.GetTemplateVersion(dc.TemplateGuid),
                    // Version = "637334794322679636", // $$$ Fix!!!
                    IsComponentLocked = false,
                    ComponentChangedSinceFileImport = true,
                    ComponentAllowCustomization = true,
                    CustomProperties = dc.CustomProperties,
                    DataComponentDefinitionKey = dc.DataComponentDefinitionKey
                    /*
                    DataComponentDefinitionKey = new DataComponentDefinitionJson
                    {
                        LogicalName = dc.Name,
                        PreferredName = dc.Name,
                        DataComponentKind = DataComponentDefinitionKind.Extension,
                        DependentEntityName = dc.DependentEntityName,
                        ControlUniqueId = controlId, // 15? 
                        DataComponentExternalDependencies = new DataComponentDataDependencyJson[]
                            {
                                new DataComponentDataDependencyJson
                            {
                                DataComponentExternalDependencyKind = DataComponentDependencyKind.Cds,
                                DataComponentCdsDependency = new CdsDataDependencyJson
                                {
                                    LogicalName = dc.DependentEntityName,
                                    DataSetName = dc.DataSetName
                                }
                            }
                            }
                    }*/
                };

                // Rehydrate fields. 
                template.DataComponentDefinitionKey.ControlUniqueId = controlId;

                dctemplate.Add(template);
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
            }


            // Rehydrate the DataComponent DataSource file. 
            {
                /*
                IEnumerable<DataComponentSourcesJson.Entry> ds =
                    from item in app._dataSources.Values.Where(x => x.IsDataComponent)
                    let dc = app.LookupDCByTemplateName(item.DataComponentTemplate)
                    select
                    new DataComponentSourcesJson.Entry
                    {
                        AssociatedDataComponentTemplate = item.DataComponentTemplate,
                        Name = item.Name,
                        Type = DataComponentSourcesJson.NativeCDSDataSourceInfo,
                        IsSampleData = false,
                        IsWritable = true,
                        DataComponentKind = "Extension",
                        DatasetName = dc.DataSetName,
                        EntitySetName = item.Name,
                        LogicalName = dc.DataSetName,
                        PreferredName = item.Name,
                        IsHidden = false,
                        DependentEntityName = dc.DependentEntityName
                    };
                    */
                IEnumerable<DataComponentSourcesJson.Entry> ds =
                   from item in app.GetDataSources().Where(x => x.IsDataComponent)
                   select item.DataComponentDetails;

                var dsArray = ds.ToArray();
                // if (dsArray.Length > 0)
                
                // backcompat-nit: if we have any DC, then always emit the DC Sources file, even if empty.
                if (dcmetadataList.Count > 0)
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
            var bytes = Encoding.UTF8.GetBytes(jsonStr);

            return new FileEntry { Name = filename, RawBytes = bytes };
        }
    }



}