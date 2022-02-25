// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas.adhoc;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl;
using Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Read/Write to a source format. 
    internal static partial class SourceSerializer
    {
        // 1 - .pa1 format
        // 2 - intro to .pa.yaml format.
        // 3 - Moved .editorstate.json files under src\EditorState
        // 4 - Moved Assets out of /Other
        // 5 - AppCheckerResult is part of Entropy
        // 6 - ScreenIndex
        // 7 - PublishOrderIndex update
        // 8 - Volatile properties to Entropy
        // 9 - Split Up ControlTemplates, subdivide src/
        // 10 - Datasource, Service defs to /pkg
        // 11 - Split out ComponentReference into its own file
        // 12 - Moved Resources.json, move volatile rootpaths to entropy
        // 13 - Control UniqueIds to Entropy
        // 14 - Yaml DoubleQuote escape
        // 15 - Use dictionary for templates
        // 16 - Group Control transform
        // 17 - Moved PublishOrderIndex entirely to Entropy 
        // 18 - AppChecker result is not part of entropy (See change 0.5 in this list) 
        // 19 - Switch extension to .fx.yaml  
        // 20 - Only load themes that match the specified theme name
        // 21 - Resourcesjson is sharded into individual json files for non-local resources.
        // 22 - AppTest is sharded into individual TestSuite.fx.yaml files in Src/Tests directory.
        // 23 - Unicodes are allowed to be part of filename and the filename is limited to 60 characters length, if it's more then it gets truncated.
        // 24 - Sharding PCF control templates in pkgs/PcfControlTemplates directory and checksum update.
        public static Version CurrentSourceVersion = new Version(0, 24);

        // Layout is:
        //  src\
        //  DataSources\
        //  Other\  (all unrecognized files)         
        public const string CodeDir = "Src";
        public const string AssetsDir = "Assets";
        public static readonly string TestDir = Path.Combine("Src", "Tests");
        public static readonly string EditorStateDir = Path.Combine("Src", "EditorState");
        public static readonly string ComponentCodeDir = Path.Combine("Src", "Components");
        public const string PackagesDir = "pkgs";
        public const string PcfControlTemplatesDir = "PcfControlTemplates";
        public static readonly string DataSourcePackageDir = Path.Combine("pkgs", "TableDefinitions");
        public static readonly string WadlPackageDir = Path.Combine("pkgs", "Wadl");
        public static readonly string SwaggerPackageDir = Path.Combine("pkgs", "Swagger");
        public static readonly string ComponentPackageDir = Path.Combine("pkgs", "Components");
        public const string OtherDir = "Other";
        public const string EntropyDir = "Entropy";
        public const string ConnectionDir = "Connections";
        public const string DataSourcesDir = "DataSources";
        public const string ComponentReferencesDir = "ComponentReferences";


        internal static readonly string AppTestControlName = "Test_7F478737223C4B69";
        internal static readonly string AppTestControlType = "AppTest";
        private static readonly string _defaultThemefileName = "Microsoft.PowerPlatform.Formulas.Tools.Themes.DefaultTheme.json";
        private static readonly string _buildVerFileName = "Microsoft.PowerPlatform.Formulas.Tools.Build.BuildVer.json";
        private static BuildVerJson _buildVerJson = GetBuildDetails();

        // Full fidelity read-write

        public static CanvasDocument LoadFromSource(string directory2, ErrorContainer errors)
        {
            if (File.Exists(directory2))
            {
                if (directory2.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
                {
                    errors.BadParameter($"Must point to a source directory, not an msapp file ({directory2}");
                }
            }

            Utilities.VerifyDirectoryExists(errors, directory2);

            if (errors.HasErrors)
            {
                return null;
            }

            var dir = new DirectoryReader(directory2);
            var app = new CanvasDocument();
            string appInsightsInstumentationKey = null;

            // Do the manifest check (and version check) first. 
            // MAnifest lives in top-level directory. 
            foreach (var file in dir.EnumerateFiles("", "*.json"))
            {
                switch (file.Kind)
                {
                    case FileKind.CanvasManifest:
                        var manifest = file.ToObject<CanvasManifestJson>();

                        if (manifest.FormatVersion != CurrentSourceVersion)
                        {
                            errors.FormatNotSupported($"This tool only supports {CurrentSourceVersion}, the manifest version is {manifest.FormatVersion}");
                            throw new DocumentException();
                        }

                        app._properties = manifest.Properties;
                        app._header = manifest.Header;
                        app._publishInfo = manifest.PublishInfo;
                        app._screenOrder = manifest.ScreenOrder;
                        break;
                    case FileKind.Templates:
                        foreach (var kvp in file.ToObject<Dictionary<string, CombinedTemplateState>>())
                        {
                            app._templateStore.AddTemplate(kvp.Key, kvp.Value);
                        }
                        break;
                    case FileKind.ComponentReferences:
                        var refs = file.ToObject<ComponentDependencyInfo[]>();
                        app._libraryReferences = refs;
                        break;
                    case FileKind.AppInsightsKey:
                        var appInsights = file.ToObject<AppInsightsKeyJson>();
                        appInsightsInstumentationKey = appInsights.InstrumentationKey;
                        break;
                }
            }
            foreach (var file in dir.EnumerateFiles("", "*.yaml"))
            {
                switch (file.Kind)
                {
                    case FileKind.Schema:
                        app._parameterSchema = file.ToObject<ParameterSchema>();
                        break;
                }
            }


            if (appInsightsInstumentationKey != null)
            {
                app._properties.InstrumentationKey = appInsightsInstumentationKey;
            }
            if (app._header == null)
            {
                // Manifest not found.
                errors.FormatNotSupported($"Can't find CanvasManifest.json file - is sources an old version?");
                throw new DocumentException();
            }

            // Load template files, recreate References/templates.json
            LoadTemplateFiles(errors, app, Path.Combine(directory2, PackagesDir), out var templateDefaults);

            // Load PowerAppsControl Templates
            LoadPcfControlTemplateFiles(errors, app, Path.Combine(directory2, PackagesDir, PcfControlTemplatesDir));

            foreach (var file in dir.EnumerateFiles(EntropyDir))
            {
                switch (file.Kind)
                {
                    case FileKind.Entropy:
                        app._entropy = file.ToObject<Entropy>();
                        break;
                    case FileKind.AppCheckerResult:
                        app._appCheckerResultJson = file.ToObject<AppCheckerResultJson>();
                        break;
                    case FileKind.Checksum:
                        app._checksum = file.ToObject<ChecksumJson>();
                        app._checksum.ClientBuildDetails = _buildVerJson;
                        break;
                    default:
                        errors.GenericWarning($"Unexpected file in Entropy, discarding");
                        break;

                }
            }

            // The resource entries for sample data is sharded into individual json files.
            // Add each of these entries back into Resrouces.json
            var resources = new List<ResourceJson>();
            app._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[0] };
            foreach (var file in dir.EnumerateFiles(AssetsDir, "*", false))
            {
                var fileEntry = file.ToFileEntry();
                if (fileEntry.Name.GetExtension() == ".json")
                {
                    // If its a json file then this must be one of the sharded files from Resources.json
                    resources.Add(file.ToObject<ResourceJson>());
                }
            }

            // Add the resources from sharded files to _resourcesJson.Resources
            if (resources.Count > 0)
            {
                app._resourcesJson.Resources = resources.ToArray();
            }

            // We have processed all the json files in Assets directory, now interate through all the files to add the asset files.
            foreach (var file in dir.EnumerateFiles(AssetsDir))
            {
                // Skip adding the json files which were created to contain the information for duplicate asset files.
                // The name of the such json files is of the format - <assetFileName>.<assetFileExtension>.json (eg. close_1.jpg.json)
                var fileName = file._relativeName;
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                // Check if the original extension was .json and the remaining file name has still got an extension,
                // Then this is an additional file that was created to contain information for duplicate assets.
                if (Path.HasExtension(fileNameWithoutExtension) && Path.GetExtension(fileName) == ".json")
                {
                    var localAssetInfoJson = file.ToObject<LocalAssetInfoJson>();
                    app._localAssetInfoJson.Add(localAssetInfoJson.NewFileName, localAssetInfoJson);
                }
                // Add non json files to _assetFiles
                else if (Path.GetExtension(fileName) != ".json")
                {
                    app.AddAssetFile(file.ToFileEntry());
                }
            }

            app.GetLogoFile();

            // Add the entries for local assets back to resrouces.json
            TranformResourceJson.AddLocalAssetEntriesToResourceJson(app);

            foreach (var file in dir.EnumerateFiles(OtherDir))
            {
                // Special files like Header / Properties 
                switch (file.Kind)
                {
                    case FileKind.Unknown:
                        // Track any unrecognized files so we can save back.
                        app.AddFile(file.ToFileEntry());
                        break;

                    default:
                        // Shouldn't find anything else not unknown in here, but just ignore them for now
                        errors.GenericWarning($"Unexpected file in Other, discarding");
                        break;

                }
            } // each loose file in '\other' 

            LoadDataSources(app, dir, errors);
            LoadSourceFiles(app, dir, templateDefaults, errors);

            foreach (var file in dir.EnumerateFiles(ConnectionDir))
            {
                // Special files like Header / Properties 
                switch (file.Kind)
                {
                    case FileKind.Connections:
                        app._connections = file.ToObject<IDictionary<string, ConnectionJson>>();
                        break;
                    default:
                        var ldr = file.ToObject<LocalDatabaseReferenceJson>();
                        app._dataSourceReferences.Add(Path.GetFileNameWithoutExtension(file._relativeName), ldr);
                        break;
                }
            }


            // Defaults. 
            // - DynamicTypes.Json, Resources.Json , Templates.Json - could all be empty
            // - Themes.json- default to

            app._idRestorer = new UniqueIdRestorer(app._entropy);

            app.OnLoadComplete(errors);

            return app;
        }

        public static CanvasDocument Create(string appName, string packagesPath, IList<string> paFiles, ErrorContainer errors)
        {
            var app = new CanvasDocument();

            app._properties = DocumentPropertiesJson.CreateDefault(appName);
            app._header = HeaderJson.CreateDefault();
            app._parameterSchema = new ParameterSchema();

            LoadTemplateFiles(errors, app, packagesPath, out var loadedTemplates);
            app._entropy = new Entropy();
            app._checksum = new ChecksumJson() { ClientStampedChecksum = "Foo", ClientBuildDetails = _buildVerJson };

            AddDefaultTheme(app);

            CreateControls(app, paFiles, loadedTemplates, errors);

            return app;
        }


        private static void LoadTemplateFiles(ErrorContainer errors, CanvasDocument app, string packagesPath, out Dictionary<string, ControlTemplate> loadedTemplates)
        {
            loadedTemplates = new Dictionary<string, ControlTemplate>();
            var templateList = new List<TemplatesJson.TemplateJson>();
            foreach (var file in new DirectoryReader(packagesPath).EnumerateFiles(string.Empty, "*.xml", searchSubdirectories: false))
            {
                var xmlContents = file.GetContents();
                if (!ControlTemplateParser.TryParseTemplate(new TemplateStore(), xmlContents, app._properties.DocumentAppType, loadedTemplates, out var parsedTemplate, out var templateName))
                {
                    errors.GenericError($"Unable to parse template file {file._relativeName}");
                    throw new DocumentException();
                }
                // Some control templates specify a name with an initial capital letter (e.g. rating control)
                // However, the server doesn't always use that. If the template name doesn't match the one we wrote
                // as the file name, adjust the template name to lowercase
                if (!file._relativeName.StartsWith(templateName))
                {
                    templateName = templateName.ToLower();
                }

                templateList.Add(new TemplatesJson.TemplateJson() { Name = templateName, Template = xmlContents, Version = parsedTemplate.Version });
            }

            var pcfTemplateConversionsList = new List<PcfTemplateJson>();
            string pcfTemplatePath = Path.Combine(packagesPath, "pcfConversions.json");
            if (File.Exists(pcfTemplatePath))
            {
                DirectoryReader.Entry file = new DirectoryReader.Entry(pcfTemplatePath);
                var pcfTemplateConversions = file.ToObject<PcfTemplateJson[]>();
                foreach (PcfTemplateJson pcfTemplateConversion in pcfTemplateConversions)
                {
                    pcfTemplateConversionsList.Add(pcfTemplateConversion);
                }                
            }
            
            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(new TemplateStore(), loadedTemplates, app._properties.DocumentAppType);

            app._templates = new TemplatesJson() { UsedTemplates = templateList.ToArray(), PcfTemplates = pcfTemplateConversionsList?.ToArray() };
        }

        private static void LoadPcfControlTemplateFiles(ErrorContainer errors, CanvasDocument app, string paControlTemplatesPath)
        {
            foreach (var file in new DirectoryReader(paControlTemplatesPath).EnumerateFiles("", "*.json"))
            {
                var pcfControl = file.ToObject<PcfControl>();
                app._pcfControls.Add(pcfControl.Name, file.ToObject<PcfControl>());
            }
        }

        // The publish info points to the logo file. Grab it from the unknowns. 
        private static void GetLogoFile(this CanvasDocument app)
        {
            // Logo file. 
            if (!string.IsNullOrEmpty(app._publishInfo?.LogoFileName))
            {
                FilePath key = FilePath.FromMsAppPath(app._publishInfo.LogoFileName);
                FileEntry logoFile;
                if (app._assetFiles.TryGetValue(key, out logoFile))
                {
                    app._unknownFiles.Remove(key);
                    app._logoFile = logoFile;
                }
                else
                {
                    throw new InvalidOperationException($"Missing logo file {key}");
                }
            }
        }

        private static void LoadSourceFiles(CanvasDocument app, DirectoryReader directory, Dictionary<string, ControlTemplate> templateDefaults, ErrorContainer errors)
        {
            foreach (var file in directory.EnumerateFiles(EditorStateDir, "*.json"))
            {
                if (!file._relativeName.EndsWith(".editorstate.json"))
                {
                    errors.FormatNotSupported($"Unexpected file present in {EditorStateDir}");
                    throw new DocumentException();
                }

                // Json peer to a .pa file.
                ControlTreeState editorState = file.ToObject<ControlTreeState>();
                if (editorState.ControlStates == null)
                    ApplyV24BackCompat(editorState, file);

                foreach (var control in editorState.ControlStates)
                {
                    control.Value.TopParentName = editorState.TopParentName;
                    if (!app._editorStateStore.TryAddControl(control.Value))
                    {
                        // Can't have duplicate control names.
                        // This might happen due to a bad merge.
                        errors.EditorStateError(file.SourceSpan, $"Control '{control.Value.Name}' is already defined.");
                    }
                }
            }

            // For now, the Themes file lives in CodeDir as a json file
            // We'd like to make this .fx.yaml as well eventually
            foreach (var file in directory.EnumerateFiles(CodeDir, "*.json", searchSubdirectories: false))
            {
                if (Path.GetFileName(file._relativeName) == "Themes.json")
                    app._themes = file.ToObject<ThemesJson>();
            }

            foreach (var file in EnumerateComponentDirs(directory, "*.fx.yaml"))
            {
                AddControl(app, file._relativeName, true, file.GetContents(), errors);

            }

            foreach (var file in EnumerateComponentDirs(directory, "*.json"))
            {
                var componentTemplate = file.ToObject<CombinedTemplateState>();
                app._templateStore.AddTemplate(componentTemplate.ComponentManifest.Name, componentTemplate);
            }

            foreach (var file in directory.EnumerateFiles(CodeDir, "*.fx.yaml", searchSubdirectories: false))
            {
                AddControl(app, file._relativeName, false, file.GetContents(), errors);
            }

            // When loading TestSuites sharded files, add them within the top parent AppTest control (i.e. Test_7F478737223C4B69)
            // Make sure to load the the Test_7F478737223C4B69.fx.yaml file first to add the top parent control.
            var shardedTestSuites = new List<DirectoryReader.Entry>();
            foreach (var file in directory.EnumerateFiles(TestDir, "*.fx.yaml"))
            {
                if (file.Kind == FileKind.AppTestParentControl)
                {
                    AddControl(app, file._relativeName, false, file.GetContents(), errors);
                }
                else
                {
                    shardedTestSuites.Add(file);
                }
            }
            shardedTestSuites.ForEach(x => AddControl(app, x._relativeName, false, x.GetContents(), errors));
        }

        // For backwards compat purposes. We may not have the new model for the
        // editor state file if the app was unpacked prior to these changes.
        // In this case, revert back to the using previous functionality.
        //
        // When SourceSerializer is updated past v24, this could be removed entirely.
        private static void ApplyV24BackCompat(ControlTreeState editorState, DirectoryReader.Entry file)
        {
            editorState.ControlStates = file.ToObject<Dictionary<string, ControlState>>();
            editorState.TopParentName = Utilities.UnEscapeFilename(file._relativeName.Replace(".editorstate.json", ""));
        }

        private static IEnumerable<DirectoryReader.Entry> EnumerateComponentDirs(
            DirectoryReader directory, string pattern)
        {
            return directory.EnumerateFiles(ComponentCodeDir, pattern).Concat(
                directory.EnumerateFiles(ComponentPackageDir, pattern));
        }

        private static void CreateControls(CanvasDocument app, IList<string> paFiles, Dictionary<string, ControlTemplate> templateDefaults, ErrorContainer errors)
        {
            foreach (var file in paFiles)
            {
                var fileEntry = new DirectoryReader.Entry(file);

                AddControl(app, file, false, fileEntry.GetContents(), errors);
            }
        }

        private static void AddControl(CanvasDocument app, string filePath, bool isComponent, string fileContents, ErrorContainer errors)
        {
            var filename = Path.GetFileName(filePath);
            try
            {
                var parser = new Parser.Parser(filePath, fileContents, errors);
                var controlIR = parser.ParseControl();
                if (controlIR == null)
                {
                    return; // error condition
                }

                // validate that all the packages refferred are not accidentally deleted from pkgs dierectory
                ValidateIfTemplateExists(app, controlIR, controlIR, errors);

                // Since the TestSuites are sharded into individual files make sure to add them as children of AppTest control
                if (AppTestTransform.IsTestSuite(controlIR.Name.Kind.TypeName))
                {
                    AddTestSuiteControl(app, controlIR);
                }
                else
                {
                    var collection = (isComponent) ? app._components : app._screens;
                    collection.Add(controlIR.Name.Identifier, controlIR);
                }
            }
            catch (DocumentException)
            {
                // On DocumentException, continue looking for errors in other files. 
            }
        }

        public static Dictionary<string, ControlTemplate> ReadTemplates(TemplatesJson templates)
        {
            throw new NotImplementedException();
        }

        // Write out to a directory (this shards it) 
        public static void SaveAsSource(CanvasDocument app, string directory2, ErrorContainer errors)
        {
            var dir = new DirectoryWriter(directory2);
            dir.DeleteAllSubdirs(errors);

            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in app._templates.UsedTemplates)
            {
                var filename = $"{template.Name}_{template.Version}.xml";
                dir.WriteAllXML(PackagesDir, new FilePath(filename), template.Template);
                if (!ControlTemplateParser.TryParseTemplate(app._templateStore, template.Template, app._properties.DocumentAppType, templateDefaults, out _, out _))
                    throw new NotSupportedException($"Unable to parse template file {template.Name}");
            }

            var pcfTemplates = new List<PcfTemplateJson>();
            // For pcf conversions 
            foreach (var template in app._templates.PcfTemplates ?? Enumerable.Empty<PcfTemplateJson>())
            {
                pcfTemplates.Add(template);                
            }
            if (pcfTemplates.Any())
            {
                dir.WriteAllJson("pkgs", new FilePath("pcfConversions.json"), pcfTemplates);
            }

            // For pcf control shard the templates
            foreach (var kvp in app._pcfControls)
            {
                dir.WriteAllJson(PackagesDir, new FilePath(PcfControlTemplatesDir, $"{kvp.Value.Name}_{kvp.Value.Version}.json"), kvp.Value);
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(app._templateStore, templateDefaults, app._properties.DocumentAppType);

            var importedComponents = app.GetImportedComponents();

            foreach (var control in app._screens)
            {
                string controlName = control.Key;
                var isTest = controlName == AppTestControlName;
                var subDir = isTest ? TestDir : CodeDir;

                WriteTopParent(dir, app, control.Key, control.Value, subDir);
            }

            foreach (var control in app._components)
            {
                string controlName = control.Key;
                app._templateStore.TryGetTemplate(controlName, out var templateState);

                bool isImported = importedComponents.Contains(templateState.TemplateOriginalName);
                var subDir = (isImported) ? ComponentPackageDir : ComponentCodeDir;
                WriteTopParent(dir, app, control.Key, control.Value, subDir);
            }

            // Write out control templates at top level, skipping component templates which are written alongside components
            var nonComponentControlTemplates = app._templateStore.Contents.Where(kvp => !(kvp.Value.IsComponentTemplate ?? false)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            dir.WriteAllJson("", new FilePath("ControlTemplates.json"), nonComponentControlTemplates);

            if (app._checksum != null)
            {
                app._checksum.ClientBuildDetails = _buildVerJson;
                dir.WriteAllJson(EntropyDir, FileKind.Checksum, app._checksum);
            }

            if (app._appCheckerResultJson != null)
            {
                dir.WriteAllJson(EntropyDir, FileKind.AppCheckerResult, app._appCheckerResultJson);
            }

            foreach (var file in app._localAssetInfoJson)
            {
                dir.WriteAllJson(AssetsDir, FilePath.FromPlatformPath(file.Value.Path), file.Value);
            }

            foreach (var file in app._assetFiles.Values)
            {
                dir.WriteAllBytes(AssetsDir, file.Name, file.RawBytes);
            }

            if (app._logoFile != null)
            {
                dir.WriteAllBytes(AssetsDir, app._logoFile.Name, app._logoFile.RawBytes);
            }

            if (app._themes != null)
            {
                dir.WriteAllJson(CodeDir, new FilePath("Themes.json"), app._themes);
            }

            if (app._resourcesJson != null)
            {
                foreach (var resource in app._resourcesJson.Resources)
                {
                    // Shard ResourceKind.Uri resources into individual json files.
                    if (resource.ResourceKind != ResourceKind.LocalFile)
                    {
                        dir.WriteAllJson(AssetsDir, new FilePath(Path.GetFileName(resource.Name) + ".json"), resource);
                    }
                }
            }

            WriteDataSources(dir, app, errors);

            // Loose files. 
            foreach (FileEntry file in app._unknownFiles.Values)
            {
                // Standardize the .json files so they're determinsitc and comparable
                if (file.Name.HasExtension(".json"))
                {
                    ReadOnlyMemory<byte> span = file.RawBytes;
                    var je = JsonDocument.Parse(span).RootElement;
                    var jsonStr = JsonNormalizer.Normalize(je);
                    dir.WriteAllText(OtherDir, file.Name, jsonStr);
                }
                else
                {
                    dir.WriteAllBytes(OtherDir, file.Name, file.RawBytes);
                }
            }

            if (app._parameterSchema != null)
            {
                dir.WriteAllJson("", FileKind.Schema, app._parameterSchema);
            }

            var manifest = new CanvasManifestJson
            {
                FormatVersion = CurrentSourceVersion,
                Properties = app._properties,
                Header = app._header,
                PublishInfo = app._publishInfo,
                ScreenOrder = app._screenOrder
            };
            dir.WriteAllJson("", FileKind.CanvasManifest, manifest);

            if (app._connections != null)
            {
                dir.WriteAllJson(ConnectionDir, FileKind.Connections, app._connections);
            }

            if (app._libraryReferences != null)
            {
                dir.WriteAllJson("", FileKind.ComponentReferences, app._libraryReferences);
            }
            if (app._appInsights != null)
            {
                dir.WriteAllJson("", FileKind.AppInsightsKey, app._appInsights);
            }

            dir.WriteAllJson(EntropyDir, FileKind.Entropy, app._entropy);
        }

        private static void WriteDataSources(DirectoryWriter dir, CanvasDocument app, ErrorContainer errors)
        {
            var untrackedLdr = app._dataSourceReferences?.Select(x => x.Key)?.ToList() ?? new List<string>();
            // Data Sources  - write out each individual source. 
            HashSet<string> filenames = new HashSet<string>();
            foreach (var kvp in app.GetDataSources())
            {
                // Filename doesn't actually matter, but careful to avoid collisions and overwriting. 
                // Also be determinstic. 
                string filename = kvp.Key + ".json";

                if (!filenames.Add(filename.ToLower()))
                {
                    int index = 1;
                    var altFileName = kvp.Key + "_" + index + ".json";
                    while (!filenames.Add(altFileName.ToLower()))
                        ++index;

                    errors.GenericWarning("Data source name collision: " + filename + ", writing as " + altFileName + " to avoid.");
                    filename = altFileName;
                }
                var dataSourceStateToWrite = kvp.Value.JsonClone().OrderBy(ds => ds.Name, StringComparer.Ordinal);
                DataSourceDefinition dataSourceDef = null;

                // Split out the changeable parts of the data source.
                foreach (var ds in dataSourceStateToWrite.Where(ds => ds.Type != "ViewInfo"))
                {
                    // CDS DataSource
                    if (ds.TableDefinition != null)
                    {
                        dataSourceDef = new DataSourceDefinition();
                        dataSourceDef.TableDefinition = Utilities.JsonParse<DataSourceTableDefinition>(ds.TableDefinition);
                        dataSourceDef.DatasetName = ds.DatasetName;
                        dataSourceDef.EntityName = ds.RelatedEntityName ?? ds.Name;
                        ds.DatasetName = null;
                        ds.TableDefinition = null;
                    }
                    // CDP DataSource
                    else if (ds.DataEntityMetadataJson != null)
                    {
                        if (ds.ApiId == "/providers/microsoft.powerapps/apis/shared_commondataservice")
                        {
                            // This is the old CDS connector, we can't support it since it's optionset format is incompatable with the newer one
                            errors.UnsupportedError($"Connection {ds.Name} is using the old CDS connector which is incompatible with this tool");
                            throw new DocumentException();
                        }
                        dataSourceDef = new DataSourceDefinition();
                        dataSourceDef.DataEntityMetadataJson = ds.DataEntityMetadataJson;
                        dataSourceDef.EntityName = ds.Name;
                        dataSourceDef.TableName = ds.TableName;
                        ds.TableName = null;
                        ds.DataEntityMetadataJson = null;
                    }
                    else if (ds.Type == "OptionSetInfo")
                    {
                        // This looks like a left over from previous versions of studio, account for it by
                        // tracking optionsets with empty dataset names
                        ds.DatasetName = ds.DatasetName == null ? string.Empty : null;
                    }
                    else if (ds.WadlMetadata != null)
                    {
                        // For some reason some connectors have both, investigate if one could be discarded by the server?
                        if (ds.WadlMetadata.WadlXml != null)
                        {
                            dir.WriteAllXML(WadlPackageDir, new FilePath(filename.Replace(".json", ".xml")), ds.WadlMetadata.WadlXml);
                        }
                        if (ds.WadlMetadata.SwaggerJson != null)
                        {
                            dir.WriteAllJson(SwaggerPackageDir, new FilePath(filename), JsonSerializer.Deserialize<SwaggerDefinition>(ds.WadlMetadata.SwaggerJson, Utilities._jsonOpts));
                        }
                        ds.WadlMetadata = null;
                    }
                }

                if (dataSourceDef != null)
                {
                    TrimViewNames(dataSourceStateToWrite, dataSourceDef.DatasetName);
                }

                if (dataSourceDef?.DatasetName != null && app._dataSourceReferences != null && app._dataSourceReferences.TryGetValue(dataSourceDef.DatasetName, out var referenceJson))
                {
                    untrackedLdr.Remove(dataSourceDef.DatasetName);
                    // copy over the localconnectionreference
                    if (referenceJson.dataSources.TryGetValue(dataSourceDef.EntityName, out var dsRef))
                    {
                        dataSourceDef.LocalReferenceDSJson = dsRef;
                    }
                    dataSourceDef.InstanceUrl = referenceJson.instanceUrl;
                    dataSourceDef.ExtensionData = referenceJson.ExtensionData;
                }

                if (dataSourceDef != null)
                    dir.WriteAllJson(DataSourcePackageDir, new FilePath(filename), dataSourceDef);

                dir.WriteAllJson(DataSourcesDir, new FilePath(filename), dataSourceStateToWrite);
            }

            foreach (var dsName in untrackedLdr)
            {
                dir.WriteAllJson(ConnectionDir, new FilePath(dsName + ".json"), app._dataSourceReferences[dsName]);
            }
        }

        private static void LoadDataSources(CanvasDocument app, DirectoryReader directory, ErrorContainer errors)
        {
            var tableDefs = new Dictionary<string, DataSourceDefinition>();
            app._dataSourceReferences = new Dictionary<string, LocalDatabaseReferenceJson>();

            foreach (var file in directory.EnumerateFiles(DataSourcePackageDir, "*.json"))
            {
                var tableDef = file.ToObject<DataSourceDefinition>();
                tableDefs.Add(tableDef.EntityName, tableDef);
                if (tableDef.DatasetName == null)
                    continue;

                if (!app._dataSourceReferences.TryGetValue(tableDef.DatasetName, out var localDatabaseReferenceJson))
                {
                    localDatabaseReferenceJson = new LocalDatabaseReferenceJson()
                    {
                        dataSources = new Dictionary<string, LocalDatabaseReferenceDataSource>(),
                        ExtensionData = tableDef.ExtensionData,
                        instanceUrl = tableDef.InstanceUrl
                    };
                    if (!app._entropy.LocalDatabaseReferencesAsEmpty)
                    {
                        app._dataSourceReferences.Add(tableDef.DatasetName, localDatabaseReferenceJson);
                    }
                }
                if (localDatabaseReferenceJson.instanceUrl != tableDef.InstanceUrl)
                {
                    // Generate an error, dataset defs have diverged in a way that shouldn't be possible
                    // Each dataset has one instanceurl
                    errors.ValidationError($"For file {file._relativeName}, the dataset {tableDef.DatasetName} has multiple instanceurls");
                    throw new DocumentException();
                }

                if (tableDef.LocalReferenceDSJson != null)
                {
                    localDatabaseReferenceJson.dataSources.Add(tableDef.EntityName, tableDef.LocalReferenceDSJson);
                }
            }

            // key is filename, value is stringified xml
            var xmlDefs = new Dictionary<string, string>();
            foreach (var file in directory.EnumerateFiles(WadlPackageDir, "*.xml"))
            {
                xmlDefs.Add(Path.GetFileNameWithoutExtension(file._relativeName), file.GetContents());
            }

            // key is filename, value is stringified json
            var swaggerDefs = new Dictionary<string, string>();
            foreach (var file in directory.EnumerateFiles(SwaggerPackageDir, "*.json"))
            {
                swaggerDefs.Add(Path.GetFileNameWithoutExtension(file._relativeName), file.GetContents());
            }

            foreach (var file in directory.EnumerateFiles(DataSourcesDir, "*", false))
            {
                var dataSources = file.ToObject<List<DataSourceEntry>>();
                foreach (var ds in dataSources)
                {
                    if (tableDefs.TryGetValue(ds.RelatedEntityName ?? ds.Name, out var definition))
                    {
                        switch (ds.Type)
                        {
                            case "NativeCDSDataSourceInfo":
                                ds.DatasetName = definition.DatasetName;
                                ds.TableDefinition = JsonSerializer.Serialize(definition.TableDefinition, Utilities._jsonOpts);
                                break;
                            case "ConnectedDataSourceInfo":
                                ds.DataEntityMetadataJson = definition.DataEntityMetadataJson;
                                ds.TableName = definition.TableName;
                                break;
                            case "OptionSetInfo":
                                ds.DatasetName = ds.DatasetName != string.Empty ? definition.DatasetName : null;
                                break;
                            case "ViewInfo":
                                if (definition != null)
                                {
                                    RestoreViewName(ds, definition.DatasetName);
                                }
                                break;
                            case "ServiceInfo":
                            default:
                                break;
                        }
                    }
                    else if (ds.Type == "ServiceInfo")
                    {
                        var foundXML = xmlDefs.TryGetValue(Path.GetFileNameWithoutExtension(file._relativeName), out string xmlDef);
                        var foundJson = swaggerDefs.TryGetValue(Path.GetFileNameWithoutExtension(file._relativeName), out string swaggerDef);

                        if (foundXML || foundJson)
                        {
                            ds.WadlMetadata = new WadlDefinition() { WadlXml = xmlDef, SwaggerJson = swaggerDef };
                        }
                    }

                    app.AddDataSourceForLoad(ds);
                }
            }
        }

        // CDS View entities have Names that start with the environment guid (datasetname)
        // This trims that from the start of the name so that all the environment-specific info
        // can be moved to the /pkg directory 
        private static void TrimViewNames(IEnumerable<DataSourceEntry> dataSourceEntries, string dataSetName)
        {
            foreach (var ds in dataSourceEntries.Where(ds => ds.Type == "ViewInfo"))
            {
                if (ds.Name.StartsWith(dataSetName))
                {
                    ds.Name = ds.Name.Substring(dataSetName.Length);
                    ds.TrimmedViewName = true;
                }
            }
        }

        // Inverse of TrimViewNames() above
        // If the name was trimmed on unpack, this reconstructs it using the
        // dataset name corresponding to the base table for the view
        private static void RestoreViewName(DataSourceEntry ds, string dataSetName)
        {
            if (ds.TrimmedViewName ?? false)
            {
                ds.Name = dataSetName + ds.Name;
                ds.TrimmedViewName = null;
            }
        }

        /// This writes out the IR, editor state cache, and potentially component templates
        /// for a single top level control, such as the App object, a screen, or component
        /// Name refers to the control name
        /// Only in case of AppTest, the topParentName is passed down, since for AppTest the TestSuites are sharded into individual files.
        /// We truncate the control names to limit it to 50 charactes length (escaped name).
        private static void WriteTopParent(
            DirectoryWriter dir,
            CanvasDocument app,
            string name,
            BlockNode ir,
            string subDir,
            string topParentName = null)
        {
            var controlName = name;
            var newControlName = Utilities.TruncateNameIfTooLong(controlName);

            string filename = newControlName + ".fx.yaml";

            // For AppTest control shard each test suite into individual files.
            if (controlName == AppTestControlName)
            {
                foreach (var child in ir.Children)
                {
                    WriteTopParent(dir, app, child.Properties.FirstOrDefault(x => x.Identifier == "DisplayName").Expression.Expression.Trim(new char[] { '"' }), child, subDir, controlName);
                }

                // Clear the children since they have already been sharded into their individual files.
                ir.Children.Clear();
            }

            var text = PAWriterVisitor.PrettyPrint(ir);
            dir.WriteAllText(subDir, filename, text);

            // For TestSuite controls, only the top parent control has an editor state created.
            // For other control types, create an editor state.
            if (string.IsNullOrEmpty(topParentName))
            {
                string editorStateFilename = $"{newControlName}.editorstate.json";

                var controlStates = new Dictionary<string, ControlState>();
                foreach (var item in app._editorStateStore.GetControlsWithTopParent(controlName))
                {
                    controlStates.Add(item.Name, item);
                }

                ControlTreeState editorState = new ControlTreeState
                {
                    ControlStates = controlStates,
                    TopParentName = controlName
                };

                // Write out of all the other state properties on the control for roundtripping.
                dir.WriteAllJson(EditorStateDir, editorStateFilename, editorState);
            }

            // Write out component templates next to the component
            if (app._templateStore.TryGetTemplate(name, out var templateState))
            {
                dir.WriteAllJson(subDir, newControlName + ".json", templateState);
            }
        }

        private static void AddDefaultTheme(CanvasDocument app)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(_defaultThemefileName);
            using var reader = new StreamReader(stream);

            var jsonString = reader.ReadToEnd();

            app._themes = JsonSerializer.Deserialize<ThemesJson>(jsonString, Utilities._jsonOpts);
        }

        private static BuildVerJson GetBuildDetails()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(_buildVerFileName);
                if (stream == null)
                {
                    return null;
                }
                using var reader = new StreamReader(stream);
                var jsonString = reader.ReadToEnd();

                return JsonSerializer.Deserialize<BuildVerJson>(jsonString, Utilities._jsonOpts);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// This method validates if the templates being references in the sources do exist.
        /// </summary>
        private static void ValidateIfTemplateExists(CanvasDocument app, BlockNode node, BlockNode root, ErrorContainer errors)
        {
            foreach (var child in node.Children)
            {
                // group, grouContainer, gallery etc. have nested controls so run the validation for all the children.
                if (child.Children?.Count > 0)
                {
                    foreach (var child1 in child.Children)
                    {
                        ValidateIfTemplateExists(app, child1, root, errors);
                    }
                }

                CombinedTemplateState templateState;
                app._templateStore.TryGetTemplate(child.Name.Kind.TypeName, out templateState);

                // Some of the child components don't have a template eg. TestStep, so we can safely continue if we can't find an entry in the templateStore.
                if (templateState == null)
                {
                    continue;
                }

                // If its a widget template then there must be a xml file in the pkgs directory.
                if (templateState.IsWidgetTemplate)
                {
                    if (!app._templates.UsedTemplates.Any(x => x.Name == child.Name.Kind.TypeName))
                    {
                        errors.ValidationError(root.SourceSpan.GetValueOrDefault(), $"Widget control template: {templateState.TemplateDisplayName}, version {templateState.Version} was not found in the pkgs directory and is referred in {root.Name.Identifier}. " +
                            $"If the template was deleted intentionally please make sure to update the source files to remove the references to this template.");
                    }
                    continue;
                }
                // if its a component template then check if the template exists in the Src/Components directory
                else if (templateState.IsComponentTemplate == true)
                {
                    if (!app._components.Keys.Any(x => x == child.Name.Kind.TypeName))
                    {
                        errors.ValidationError(root.SourceSpan.GetValueOrDefault(), $"Component template: {templateState.TemplateDisplayName} was not found in Src/Components directory and is referred in {root.Name.Identifier}. " +
                            $"If the template was deleted intentionally please make sure to update the source files to remove the references to this template.");
                    }
                    continue;
                }
            }
        }

        /// Adds TestSuite as a child control of AppTest control
        private static void AddTestSuiteControl(CanvasDocument app, BlockNode controlIR)
        {
            if (!app._screens.ContainsKey(AppTestControlName))
            {
                app._screens.Add(AppTestControlName, new BlockNode()
                {
                    Name = new TypedNameNode()
                    {
                        Identifier = AppTestControlName,
                        Kind = new TypeNode() { TypeName = AppTestControlType }
                    }
                });
            }
            app._screens[AppTestControlName].Children.Add(controlIR);
        }
    }
}
