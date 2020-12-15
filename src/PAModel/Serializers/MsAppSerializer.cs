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
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Read/Write to an .msapp file. 
    internal static class MsAppSerializer
    {
        private static T ToObject<T>(ZipArchiveEntry entry)
        {
            var je = entry.ToJson();
            return je.ToObject<T>();
        }
        
        public static CanvasDocument Load(string fullpathToMsApp, ErrorContainer errors)
        {
            if (!fullpathToMsApp.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only works for .msapp files");
            }

            // Read raw files. 
            // Apply transforms. 
            var app = new CanvasDocument();

            app._checksum = new ChecksumJson(); // default empty. Will get overwritten if the file is present.
            app._templateStore = new EditorState.TemplateStore();
            app._editorStateStore = new EditorState.EditorStateStore();

            ComponentsMetadataJson componentsMetadata = null;
            DataComponentTemplatesJson dctemplate = null;
            DataComponentSourcesJson dcsources  = null;

            //string actualChecksum = ChecksumMaker.GetChecksum(fullpathToMsApp);

            ChecksumMaker checksumMaker = new ChecksumMaker();
            // app._checksum

            using (var z = ZipFile.OpenRead(fullpathToMsApp))
            {
                foreach (var entry in z.Entries)
                {
                    checksumMaker.AddFile(entry.FullName, entry.ToBytes());

                    var fullName = entry.FullName;
                    var kind = FileEntry.TriageKind(fullName);

                    switch (kind)
                    {
                        default:
                            // Track any unrecognized files so we can save back.
                            app.AddFile(FileEntry.FromZip(entry));
                            break;

                        case FileKind.Asset:
                            app.AddAssetFile(FileEntry.FromZip(entry, name: fullName.Substring("Assets\\".Length)));
                            break;

                        case FileKind.Checksum:
                            app._checksum = ToObject<ChecksumJson>(entry);
                            break;

                        case FileKind.OldEntityJSon:
                            errors.FormatNotSupported($"This is using an older v1 msapp format that is not supported.");
                            throw new DocumentException();

                        case FileKind.DataComponentTemplates:
                            dctemplate = ToObject<DataComponentTemplatesJson>(entry);
                            break;
                        case FileKind.ComponentsMetadata:
                            componentsMetadata = ToObject<ComponentsMetadataJson>(entry);
                            break;
                        case FileKind.DataComponentSources:
                            dcsources = ToObject<DataComponentSourcesJson>(entry);
                            break;

                        case FileKind.Properties:
                            app._properties = ToObject<DocumentPropertiesJson>(entry);
                            break;
                        case FileKind.Themes:
                            app._themes = ToObject<ThemesJson>(entry);
                            break;
                        case FileKind.Header:
                            app._header = ToObject<HeaderJson>(entry);
                            app._entropy.SetHeaderLastSaved(app._header.LastSavedDateTimeUTC);
                            app._header.LastSavedDateTimeUTC = null;
                            break;

                        case FileKind.PublishInfo:
                            app._publishInfo = ToObject<PublishInfoJson>(entry);
                            break;

                        case FileKind.AppCheckerResult:
                            var appChecker = Encoding.UTF8.GetString(entry.ToBytes());
                            app._entropy.AppCheckerResult = appChecker;
                            break;
                                                    

                        case FileKind.ComponentSrc:
                        case FileKind.ControlSrc:
                        case FileKind.TestSrc:
                            {
                                var control = ToObject<ControlInfoJson>(entry);
                                var sf = SourceFile.New(control);
                                IRStateHelpers.SplitIRAndState(sf, app._editorStateStore, app._templateStore, out var controlIR);
                                app._sources.Add(sf.ControlName, controlIR);
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
                        case FileKind.Templates:
                            {
                                app._templates = ToObject<TemplatesJson>(entry);
                                int iOrder = 0;
                                foreach (var template in app._templates.UsedTemplates)
                                {
                                    app._entropy.Add(template, iOrder);
                                    iOrder++;
                                }
                                iOrder = 0;
                                foreach (var template in app._templates.ComponentTemplates ?? Enumerable.Empty<TemplateMetadataJson>())
                                {
                                    app._entropy.AddComponent(template, iOrder);
                                    iOrder++;
                                }
                            }
                            break;                                
                    }
                } // foreach zip entry

                foreach (var componentTemplate in app._templates.ComponentTemplates ?? Enumerable.Empty<TemplateMetadataJson>())
                {
                    if (!app._templateStore.TryGetTemplate(componentTemplate.Name, out var template))
                        continue;
                    template.TemplateOriginalName = componentTemplate.OriginalName;
                    template.IsComponentLocked = componentTemplate.IsComponentLocked;
                    template.ComponentChangedSinceFileImport = componentTemplate.ComponentChangedSinceFileImport;
                    template.ComponentAllowCustomization = componentTemplate.ComponentAllowCustomization;

                    if (template.Version != componentTemplate.Version)
                    {
                        app._entropy.SetTemplateVersion(template.Name, componentTemplate.Version);
                    }
                }


                // Checksums?
                var currentChecksum = checksumMaker.GetChecksum();
                if (app._checksum.ClientStampedChecksum != null && app._checksum.ClientStampedChecksum != currentChecksum)
                {
                    // The server checksum doesn't match the actual contents. 
                    // likely has been tampered.
                    errors.ChecksumMismatch("Checksum doesn't match on extract.");
                }
                app._checksum.ClientStampedChecksum = currentChecksum;

                // Normalize logo filename. 
                app.TranformLogoOnLoad();

                if (app._properties.LocalConnectionReferences != null)  
                {
                    var cxs = Utility.JsonParse<IDictionary<String, ConnectionJson>>(app._properties.LocalConnectionReferences);
                    app._connections = cxs;
                    app._properties.LocalConnectionReferences = null;
                }

                if (componentsMetadata?.Components != null)
                {
                    int order = 0;
                    foreach (var x in componentsMetadata.Components)
                    {
                        var manifest = ComponentManifest.Create(x);
                        if (!app._templateStore.TryGetTemplate(x.TemplateName, out var templateState))
                        {
                            errors.FormatNotSupported("Component Metadata contains template not present in the app");
                            throw new DocumentException();
                        }

                        templateState.ComponentManifest = manifest;
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
                        if (x.ComponentType == null)
                        {
                            errors.FormatNotSupported($"Data component {x.Name} is using an outdated format");
                            throw new DocumentException();
                        }

                        if (!app._templateStore.TryGetTemplate(x.Name, out var templateState))
                        {
                            errors.FormatNotSupported("Component Metadata contains template not present in the app");
                            throw new DocumentException();
                        }

                        ComponentManifest manifest = templateState.ComponentManifest; // Should already exist
                        app._entropy.SetTemplateVersion(x.Name, x.Version);
                        app._entropy.Add(x, order);
                        manifest.Apply(x);
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

            app.ApplyAfterMsAppLoadTransforms(errors);
            app.OnLoadComplete(errors);

            return app;
        }

        internal static void AddFile(this CanvasDocument app, FileEntry entry)
        {
            app._unknownFiles.Add(entry.Name, entry);
        }

        internal static void AddAssetFile(this CanvasDocument app, FileEntry entry)
        {
            app._assetFiles.Add(entry.Name, entry);
        }

        // Write back out to a msapp file. 
        public static void SaveAsMsApp(CanvasDocument app, string fullpathToMsApp, ErrorContainer errors)
        {
            app.ApplyBeforeMsAppWriteTransforms(errors);

            if (!fullpathToMsApp.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase) &&
                fullpathToMsApp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                
                throw new InvalidOperationException("Only works for .msapp files");
            }

            if (File.Exists(fullpathToMsApp)) // Overwrite!
            {
                File.Delete(fullpathToMsApp);
            }


            var checksum = new ChecksumMaker();

            DirectoryWriter.EnsureFileDirExists(fullpathToMsApp);
            using (var z = ZipFile.Open(fullpathToMsApp, ZipArchiveMode.Create))
            {
                foreach (FileEntry entry in app.GetMsAppFiles(errors))
                {
                    if (entry != null)
                    {
                        var e = z.CreateEntry(entry.Name);
                        using (var dest = e.Open())
                        {
                            dest.Write(entry.RawBytes, 0, entry.RawBytes.Length);
                            checksum.AddFile(entry.Name, entry.RawBytes);
                        }
                    }
                }

                ComputeAndWriteChecksum(app, checksum, z, errors);
            }

            // Undo BeforeWrite transforms so CanvasDocument representation is unchanged
            app.ApplyAfterMsAppLoadTransforms(errors);
        }

        private static void ComputeAndWriteChecksum(CanvasDocument app, ChecksumMaker checksum, ZipArchive z, ErrorContainer errors)
        {
            var hash = checksum.GetChecksum();


            if (hash != app._checksum.ClientStampedChecksum)
            {
                // We had offline edits!
                errors.ChecksumMismatch("Sources have changed since when they were unpacked.");
            }

            var checksumJson = new ChecksumJson
            {
                ClientStampedChecksum = hash,
                ServerStampedChecksum = app._checksum.ServerStampedChecksum
            };

            var entry = ToFile(FileKind.Checksum, checksumJson);
            var e = z.CreateEntry(entry.Name);
            using (var dest = e.Open())
            {
                dest.Write(entry.RawBytes, 0, entry.RawBytes.Length);
            }
        }

        // Get everything that should be stored as a file in the .msapp.
        private static IEnumerable<FileEntry> GetMsAppFiles(this CanvasDocument app, ErrorContainer errors)
        {
            // Loose files
            foreach (var file in app._unknownFiles.Values)
            {
                yield return file;
            }

            yield return ToFile(FileKind.Themes, app._themes);

            var header = app._header.JsonClone();
            header.LastSavedDateTimeUTC = app._entropy.GetHeaderLastSaved();
            yield return ToFile(FileKind.Header, header);

            
            var props = app._properties.JsonClone();
            if (app._connections != null)
            {
                var json = Utility.JsonSerialize(app._connections);
                props.LocalConnectionReferences = json;
            }
            yield return ToFile(FileKind.Properties, props);

            var (publishInfo, logoFile) = app.TransformLogoOnSave();
            yield return logoFile;

            if (publishInfo != null)
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

            var sourceFiles = new List<SourceFile>();

            // Rehydrate sources before yielding any to be written, processing component defs first
            foreach (var controlData in app._sources
                .OrderBy(source =>
                    (app._editorStateStore.TryGetControlState(source.Value.Name.Identifier, out var control) &&
                    (control.IsComponentDefinition ?? false)) ? -1 : 1))
            {
                var sourceFile = IRStateHelpers.CombineIRAndState(controlData.Value, errors, app._editorStateStore, app._templateStore);
                sourceFiles.Add(sourceFile);
            }

            foreach (var sourceFile in sourceFiles)
            {
                yield return sourceFile.ToMsAppFile();
            }

            var componentTemplates = new List<TemplateMetadataJson>();
            foreach (var template in app._templateStore.Contents.Where(template => template.Value.IsComponentTemplate ?? false))
            {
                if (((template.Value.CustomProperties?.Any() ?? false) || template.Value.ComponentAllowCustomization.HasValue) &&
                    !(template.Value.ComponentManifest?.IsDataComponent ?? false))
                {
                    componentTemplates.Add(template.Value.ToTemplateMetadata(app._entropy));
                }
            }

            app._templates = new TemplatesJson()
            {
                ComponentTemplates = componentTemplates.Any() ? componentTemplates.OrderBy(x => app._entropy.GetComponentOrder(x)).ToArray() : null,
                UsedTemplates = app._templates.UsedTemplates.OrderBy(x => app._entropy.GetOrder(x)).ToArray()
            };

            yield return ToFile(FileKind.Templates, app._templates);

            var componentsMetadata = new List<ComponentsMetadataJson.Entry>();
            var dctemplate = new List<TemplateMetadataJson>();


            foreach (var componentTemplate in app._templateStore.Contents.Values.Where(state => state.ComponentManifest != null))
            {
                var manifest = componentTemplate.ComponentManifest;
                componentsMetadata.Add(new ComponentsMetadataJson.Entry
                {
                    Name = manifest.Name,
                    TemplateName = manifest.TemplateGuid,
                    ExtensionData = manifest.ExtensionData
                });

                if (manifest.IsDataComponent)
                {
                    var controlId = GetDataComponentDefinition(sourceFiles.Select(source => source.Value), manifest.TemplateGuid, errors).ControlUniqueId;

                    var template = new TemplateMetadataJson
                    {
                        Name = manifest.TemplateGuid,
                        ComponentType = ComponentType.DataComponent,
                        Version = app._entropy.GetTemplateVersion(manifest.TemplateGuid),
                        IsComponentLocked = false,
                        ComponentChangedSinceFileImport = true,
                        ComponentAllowCustomization = true,
                        CustomProperties = componentTemplate.CustomProperties,
                        DataComponentDefinitionKey = manifest.DataComponentDefinitionKey
                    };

                    // Rehydrate fields. 
                    template.DataComponentDefinitionKey.ControlUniqueId = controlId;

                    dctemplate.Add(template);
                }
            }

            if (componentsMetadata.Count > 0)
            {
                // If the components file is present, then write out all files. 
                yield return ToFile(FileKind.ComponentsMetadata, new ComponentsMetadataJson
                {
                    Components = componentsMetadata
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

            if (app._entropy?.AppCheckerResult != null)
            {
                yield return new FileEntry() { Name = FileEntry.GetFilenameForKind(FileKind.AppCheckerResult), RawBytes = Encoding.UTF8.GetBytes(app._entropy.AppCheckerResult) };
            }

            foreach (var assetFile in app._assetFiles)
            {
                yield return new FileEntry { Name = @"Assets\" + assetFile.Value.Name, RawBytes = assetFile.Value.RawBytes };
            }
        }

        private static ControlInfoJson.Item GetDataComponentDefinition(IEnumerable<ControlInfoJson> topParents, string templateGuid, ErrorContainer errors)
        {
            foreach (var topParent in topParents)
            {
                if (topParent.TopParent.Template.Name == templateGuid)
                {
                    return topParent.TopParent;
                }
            }
            errors.GenericError("Could not find DataComponent Definition for template " + templateGuid);
            throw new DocumentException();
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
