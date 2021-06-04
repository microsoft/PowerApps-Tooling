// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

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

        public static CanvasDocument Load(Stream streamToMsapp, ErrorContainer errors)
        {
            if (streamToMsapp == null)
            {
                throw new ArgumentNullException(nameof(streamToMsapp));
            }

            // Read raw files. 
            // Apply transforms. 
            var app = new CanvasDocument();

            app._checksum = new ChecksumJson(); // default empty. Will get overwritten if the file is present.
            app._templateStore = new EditorState.TemplateStore();
            app._editorStateStore = new EditorState.EditorStateStore();

            ComponentsMetadataJson componentsMetadata = null;
            DataComponentTemplatesJson dctemplate = null;
            DataComponentSourcesJson dcsources = null;

            ChecksumMaker checksumMaker = new ChecksumMaker();
            // key = screen, value = index
            var screenOrder = new Dictionary<string, double>();

            ZipArchive zipOpen;
            try
            {
                zipOpen = new ZipArchive(streamToMsapp, ZipArchiveMode.Read);
            }
            catch (Exception e)
            {
                // Catch cases where stream is corrupted, can't be read, or unavailable.
                errors.MsAppFormatError(e.Message);
                return null;
            }

            using (var z = zipOpen)
            {
                foreach (var entry in z.Entries)
                {
                    checksumMaker.AddFile(FileEntry.FromZip(entry).Name.ToMsAppPath(), entry.ToBytes());

                    var fullName = entry.FullName;
                    var kind = FileEntry.TriageKind(FilePath.FromMsAppPath(fullName));

                    switch (kind)
                    {
                        default:
                            // Track any unrecognized files so we can save back.
                            app.AddFile(FileEntry.FromZip(entry));
                            break;

                        case FileKind.Resources:
                            app._resourcesJson = ToObject<ResourcesJson>(entry);
                            foreach (var resource in app._resourcesJson.Resources)
                            {
                                if (resource.ResourceKind == ResourceKind.LocalFile)
                                {
                                    app._entropy.LocalResourceRootPaths.Add(resource.Name, resource.RootPath);
                                    resource.RootPath = null;
                                }
                            }

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
                            app._appCheckerResultJson = ToObject<AppCheckerResultJson>(entry);
                            break;
                        case FileKind.ComponentSrc:
                        case FileKind.ControlSrc:
                        case FileKind.TestSrc:
                            {
                                var control = ToObject<ControlInfoJson>(entry);
                                var sf = SourceFile.New(control);
                                // Add to screen order, only screens have meaningful indices, components may have collisions
                                if (!ExcludeControlFromScreenOrdering(sf))
                                {
                                    screenOrder.Add(control.TopParent.Name, control.TopParent.Index);
                                }
                                var flattenedControlTree = sf.Flatten();

                                foreach (var ctrl in flattenedControlTree)
                                {
                                    // Add PublishOrderIndex to Entropy so it doesn't affect the editorstate diff.
                                    app._entropy.PublishOrderIndices.Add(ctrl.Name, ctrl.PublishOrderIndex);

                                    // For component instances, also track their index in Entropy
                                    if (ctrl.Index == 0.0 || ctrl.Template.Id == "http://microsoft.com/appmagic/screen")
                                        continue;
                                    app._entropy.ComponentIndexes.Add(ctrl.Name, ctrl.Index);
                                }

                                IRStateHelpers.SplitIRAndState(sf, app._editorStateStore, app._templateStore, app._entropy, out var controlIR);
                                if (kind == FileKind.ComponentSrc)
                                    app._components.Add(sf.ControlName, controlIR);
                                else
                                    app._screens.Add(sf.ControlName, controlIR);
                            }
                            break;


                        case FileKind.DataSources:
                            {
                                var dataSources = ToObject<DataSourcesJson>(entry);
                                Utilities.EnsureNoExtraData(dataSources.ExtensionData);

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

                foreach (var template in app._templates.UsedTemplates)
                {
                    if (app._templateStore.TryGetTemplate(template.Name, out var templateState))
                    {
                        templateState.IsWidgetTemplate = true;
                    }
                }

                foreach (var componentTemplate in app._templates.ComponentTemplates ?? Enumerable.Empty<TemplateMetadataJson>())
                {
                    if (!app._templateStore.TryGetTemplate(componentTemplate.Name, out var template))
                        continue;
                    template.TemplateOriginalName = componentTemplate.OriginalName;
                    template.IsComponentLocked = componentTemplate.IsComponentLocked;
                    template.ComponentChangedSinceFileImport = componentTemplate.ComponentChangedSinceFileImport;
                    template.ComponentAllowCustomization = componentTemplate.ComponentAllowCustomization;
                    template.ComponentExtraMetadata = componentTemplate.ExtensionData;

                    if (template.Version != componentTemplate.Version)
                    {
                        app._entropy.SetTemplateVersion(template.Name, componentTemplate.Version);
                    }
                }

                app._screenOrder = screenOrder.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

                // Checksums?
                var currentChecksum = checksumMaker.GetChecksum();

                // This is debug only. The server checksum is out of date with the client checksum
                // The main checksum validation that matters is the repack after unpack
#if DEBUG
                var isNullOrOlderChecksum = app._checksum.ServerStampedChecksum == null
                                ? true
                                : int.Parse(app._checksum.ServerStampedChecksum.Split('_').First(), NumberStyles.HexNumber) < int.Parse(ChecksumMaker.Version, NumberStyles.HexNumber);
                if (!isNullOrOlderChecksum &&  app._checksum.ServerStampedChecksum != currentChecksum.wholeChecksum)
                {
                    // The server checksum doesn't match the actual contents. 
                    // likely has been tampered.
                    errors.ChecksumMismatch("Checksum doesn't match on extract.");
                    if (app._checksum.ServerPerFileChecksums != null)
                    {
                        foreach (var file in app._checksum.ServerPerFileChecksums)
                        {
                            if (!currentChecksum.perFileChecksum.TryGetValue(file.Key, out var fileChecksum))
                            {
                                errors.ChecksumMismatch("Missing file " + file.Key);
                            }
                            else if (fileChecksum != file.Value)
                            {
                                errors.ChecksumMismatch($"File {file.Key} checksum does not match on extract");
                            }
                        }
                        foreach (var file in currentChecksum.perFileChecksum)
                        {
                            if (!app._checksum.ServerPerFileChecksums.ContainsKey(file.Key))
                            {
                                errors.ChecksumMismatch("Extra file " + file.Key);
                            }
                        }
                    }
                }
#endif

                app._checksum.ClientStampedChecksum = currentChecksum.wholeChecksum;
                app._checksum.ClientPerFileChecksums = currentChecksum.perFileChecksum;
                // Normalize logo filename. 
                app.TranformLogoOnLoad();

                if (!string.IsNullOrEmpty(app._properties.LibraryDependencies))
                {
                    var refs = Utilities.JsonParse<ComponentDependencyInfo[]>(app._properties.LibraryDependencies);
                    app._libraryReferences = refs;
                    app._properties.LibraryDependencies = null;
                }

                if (!string.IsNullOrEmpty(app._properties.LocalConnectionReferences))
                {
                    var cxs = Utilities.JsonParse<IDictionary<String, ConnectionJson>>(app._properties.LocalConnectionReferences);
                    app._connections = cxs;
                    app._properties.LocalConnectionReferences = null;
                }

                if (!string.IsNullOrEmpty(app._properties.LocalDatabaseReferences))
                {
                    var dsrs = Utilities.JsonParse<IDictionary<String, LocalDatabaseReferenceJson>>(app._properties.LocalDatabaseReferences);
                    app._dataSourceReferences = dsrs;
                    app._properties.LocalDatabaseReferences = null;
                    app._entropy.LocalDatabaseReferencesAsEmpty = false;
                }
                else
                {
                    app._entropy.LocalDatabaseReferencesAsEmpty = true;
                }

                if (app._properties.InstrumentationKey != null)
                {
                    app._appInsights = new AppInsightsKeyJson() { InstrumentationKey = app._properties.InstrumentationKey };
                    app._properties.InstrumentationKey = null;
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
        public static void SaveAsMsApp(CanvasDocument app, string fullpathToMsApp, ErrorContainer errors, bool isValidation = false)
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
                        var e = z.CreateEntry(entry.Name.ToMsAppPath());
                        using (var dest = e.Open())
                        {
                            dest.Write(entry.RawBytes, 0, entry.RawBytes.Length);
                            checksum.AddFile(entry.Name.ToMsAppPath(), entry.RawBytes);
                        }
                    }
                }

                ComputeAndWriteChecksum(app, checksum, z, errors, isValidation);
            }

            // Undo BeforeWrite transforms so CanvasDocument representation is unchanged
            app.ApplyAfterMsAppLoadTransforms(errors);
        }

        private static void ComputeAndWriteChecksum(CanvasDocument app, ChecksumMaker checksum, ZipArchive z, ErrorContainer errors, bool isValidation)
        {
            var hash = checksum.GetChecksum();

            if (app._checksum != null && hash.wholeChecksum != app._checksum.ClientStampedChecksum)
            {
                // These warnings are Debug only. Throwing a bunch of warning messages at the customer could lead to them ignoring real errors.
#if DEBUG
                if (app._checksum.ClientPerFileChecksums != null)
                {
                    foreach (var file in app._checksum.ClientPerFileChecksums)
                    {
                        if (!hash.perFileChecksum.TryGetValue(file.Key, out var fileChecksum))
                        {
                            errors.ChecksumMismatch("Missing file " + file.Key);
                        }
                        else if (fileChecksum != file.Value)
                        {
                            errors.ChecksumMismatch($"File {file.Key} checksum does not match on extract");
                        }
                    }
                    foreach (var file in hash.perFileChecksum)
                    {
                        if (!app._checksum.ClientPerFileChecksums.ContainsKey(file.Key))
                        {
                            errors.ChecksumMismatch("Extra file " + file.Key);
                        }
                    }
                }

#endif

                // These are the non-debug warnings, if it's unpack this was a serious error, on -pack it's most likely not
                if (isValidation)
                {
                    errors.PostUnpackValidationFailed();
                    throw new DocumentException();
                }

                errors.ChecksumMismatch("Checksum indicates that sources have been edited since they were unpacked. If this was intentional, ignore this warning.");
            }

            var checksumJson = new ChecksumJson
            {
                ClientStampedChecksum = hash.wholeChecksum,
                ClientPerFileChecksums = hash.perFileChecksum,
                ServerStampedChecksum = app._checksum?.ServerStampedChecksum,
                ServerPerFileChecksums = app._checksum?.ServerPerFileChecksums,
            };

            var entry = ToFile(FileKind.Checksum, checksumJson);
            var e = z.CreateEntry(entry.Name.ToMsAppPath());
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
                var json = Utilities.JsonSerialize(app._connections);
                props.LocalConnectionReferences = json;
            }
            if (app._appInsights != null)
            {
                props.InstrumentationKey = app._appInsights.InstrumentationKey;
            }
            if (app._dataSourceReferences != null)
            {
                var json = Utilities.JsonSerialize(app._dataSourceReferences);

                // Some formats serialize empty as "", some serialize as "{}"
                if (app._dataSourceReferences.Count == 0)
                {
                    if (app._entropy.LocalDatabaseReferencesAsEmpty)
                    {
                        json = "";
                    }
                    else
                    {
                        json = "{}";
                    }
                }

                props.LocalDatabaseReferences = json;
            }

            if (app._libraryReferences != null)
            {
                var json = Utilities.JsonSerialize(app._libraryReferences);
                props.LibraryDependencies = json;
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
                    .SelectMany(x => x.Value)
                    .Where(x => !x.IsDataComponent)
                    .OrderBy(x => app._entropy.GetOrder(x))
                    .ToArray()
            };
            yield return ToFile(FileKind.DataSources, dataSources);

            var sourceFiles = new List<SourceFile>();



            var idRestorer = new UniqueIdRestorer(app._entropy);
            var maxPublishOrderIndex = app._entropy.PublishOrderIndices.Any() ? app._entropy.PublishOrderIndices.Values.Max() : 0;
            // Rehydrate sources before yielding any to be written, processing component defs first
            foreach (var controlData in app._screens.Concat(app._components)
                .OrderBy(source =>
                    (app._editorStateStore.TryGetControlState(source.Value.Name.Identifier, out var control) &&
                    (control.IsComponentDefinition ?? false)) ? -1 : 1))
            {
                var sourceFile = IRStateHelpers.CombineIRAndState(controlData.Value, errors, app._editorStateStore, app._templateStore, idRestorer, app._entropy);
                // Offset the publishOrderIndex based on Entropy.json
                foreach (var ctrl in sourceFile.Flatten())
                {
                    if (app._entropy.PublishOrderIndices.TryGetValue(ctrl.Name, out var index))
                    {
                        ctrl.PublishOrderIndex = index;
                    }
                    else
                    {
                        ctrl.PublishOrderIndex = ++maxPublishOrderIndex;
                    }
                }
                sourceFiles.Add(sourceFile);
            }

            CheckUniqueIds(errors, sourceFiles);

            // Repair order when screens are unchanged
            if (sourceFiles.Where(file => !ExcludeControlFromScreenOrdering(file)).Count() == app._screenOrder.Count &&
               sourceFiles.Where(file => !ExcludeControlFromScreenOrdering(file)).All(file => app._screenOrder.Contains(file.ControlName)))
            {
                double i = 0.0;
                foreach (var screen in app._screenOrder)
                {
                    sourceFiles.First(file => file.ControlName == screen).Value.TopParent.Index = i;
                    i += 1;
                }
            }
            else
            {
                // Make up an order, it doesn't really matter.
                double i = 0.0;
                foreach (var sourceFile in sourceFiles)
                {
                    if (ExcludeControlFromScreenOrdering(sourceFile))
                        continue;
                    sourceFile.Value.TopParent.Index = i;
                    i += 1;
                }
            }

            RepairComponentInstanceIndex(app._entropy?.ComponentIndexes ?? new Dictionary<string, double>(), sourceFiles);


            // This ordering is essential, we need to match the order in which Studio writes the files to replicate certain order-dependent behavior.
            foreach (var sourceFile in sourceFiles.OrderBy(file => file.GetMsAppFilename()))
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
                   from item in app.GetDataSources().SelectMany(x => x.Value).Where(x => x.IsDataComponent)
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

            if (app._appCheckerResultJson != null)
            {
                yield return ToFile(FileKind.AppCheckerResult, app._appCheckerResultJson);
            }

            if (app._resourcesJson != null)
            {
                var resources = app._resourcesJson.JsonClone();
                foreach (var resource in resources.Resources)
                {
                    if (resource.ResourceKind == ResourceKind.LocalFile)
                    {
                        var rootPath = string.Empty;
                        if (app._entropy?.LocalResourceRootPaths.TryGetValue(resource.Name, out rootPath) ?? false)
                            resource.RootPath = rootPath;
                        else
                            resource.RootPath = string.Empty;
                    }
                }
                yield return ToFile(FileKind.Resources, resources);
            }

            foreach (var assetFile in app._assetFiles)
            {
                yield return new FileEntry { Name = FilePath.RootedAt("Assets", assetFile.Value.Name), RawBytes = assetFile.Value.RawBytes };
            }
        }

        private static void CheckUniqueIds(ErrorContainer errors, List<SourceFile> sourceFiles)
        {
            var uniqueIds = new HashSet<string>();
            foreach (var sourceFile in sourceFiles)
            {
                foreach (var ctrl in sourceFile.Flatten())
                {
                    if (!uniqueIds.Add(ctrl.ControlUniqueId))
                    {
                        errors.GenericMsAppError("Duplicate Control Unique Ids");
                    }
                }
            }
        }

        private static bool ExcludeControlFromScreenOrdering(SourceFile file)
        {
            return file.Kind != SourceKind.Control || file.ControlName == "App";
        }

        private static void RepairComponentInstanceIndex(Dictionary<string, double> componentIndices, List<SourceFile> files)
        {
            foreach (var control in files.SelectMany(file => file.Flatten()))
            {
                if (componentIndices.TryGetValue(control.Name, out var index))
                    control.Index = index;
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

            var jsonStr = JsonSerializer.Serialize(value, Utilities._jsonOpts);

            jsonStr = JsonNormalizer.Normalize(jsonStr);

            var bytes = Encoding.UTF8.GetBytes(jsonStr);

            return new FileEntry { Name = filename, RawBytes = bytes };
        }
    }
}
