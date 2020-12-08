// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Read/Write to a source format. 
    internal static partial class SourceSerializer
    {
        // 1 - .pa1 format
        // 2 - intro to .pa.yaml format.
        // 3 - Moved .editorstate.json files under src\EditorState
        public static Version CurrentSourceVersion = new Version(0, 3);

        // Layout is:
        //  src\
        //  DataSources\
        //  Other\  (all unrecognized files)         
        public const string CodeDir = "Src";
        public const string EditorStateDir = "Src\\EditorState";
        public const string PackagesDir = "pkgs";
        public const string OtherDir = "Other"; // exactly match files from .msapp format
        public const string ConnectionDir = "Connections";
        public const string DataSourcesDir = "DataSources";
        public const string Ignore = "Ignore"; // Write-only, ignore these files.

        private static readonly string _defaultThemefileName = "Microsoft.PowerPlatform.Formulas.Tools.Themes.DefaultTheme.json";

        private static T ToObject<T>(string fullpath)
        {
            var str = File.ReadAllText(fullpath);
            return JsonSerializer.Deserialize<T>(str, Utility._jsonOpts);
        }


        // Full fidelity read-write

        public static CanvasDocument LoadFromSource(string directory2, ErrorContainer errors)
        {
            if (File.Exists(directory2))
            {
                if (directory2.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Must point to a source directory, not an msapp file ({directory2}");
                }
            }

            if (!Directory.Exists(directory2))
            {
                throw new InvalidOperationException($"No directory {directory2}");
            }
            var dir = new DirectoryReader(directory2);
            
            // $$$ Duplicate with MsAppSerializer? 
            var app = new CanvasDocument();

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
                        break;
                }
            }
            if (app._header == null)
            {
                // Manifest not found.
                errors.FormatNotSupported($"Can't find CanvasManifest.json file - is sources an old version?");
                throw new DocumentException();
            }

            // Load template files, recreate References/templates.json
            LoadTemplateFiles(app, Path.Combine(directory2, PackagesDir), out var templateDefaults);

            // var root = Path.Combine(directory, OtherDir);
            foreach (var file in dir.EnumerateFiles(OtherDir))
            {
                // Special files like Header / Properties 
                switch (file.Kind)
                {
                    default:
                        // Track any unrecognized files so we can save back.
                        app.AddFile(file.ToFileEntry());
                        break;

                    case FileKind.Entropy:
                        app._entropy = file.ToObject<Entropy>();
                        break;

                    case FileKind.Checksum:
                        app._checksum = file.ToObject<ChecksumJson>();
                        break;

                    case FileKind.Themes:
                        app._themes = file.ToObject<ThemesJson>();
                        break;

                    case FileKind.Header:
                    case FileKind.Properties:
                        throw new NotSupportedException($"Old format");

                    case FileKind.ComponentSrc:
                    case FileKind.ControlSrc:                        
                        // Shouldn't find any here -  were explicit in source
                        throw new InvalidOperationException($"Unexpected source file: " + file._relativeName);
                        
                }
            } // each loose file in '\other' 


            app.GetLogoFileFromUnknowns();

            LoadDataSources(app, dir);
            LoadSourceFiles(app, dir, templateDefaults, errors);

            foreach (var file in dir.EnumerateFiles(ConnectionDir))
            {
                // Special files like Header / Properties 
                switch (file.Kind)
                {
                    case FileKind.Connections:
                        app._connections = file.ToObject<IDictionary<string, ConnectionJson>>();
                        break;
                }
            }


            // Defaults. 
            // - DynamicTypes.Json, Resources.Json , Templates.Json - could all be empty
            // - Themes.json- default to


            app.OnLoadComplete();

            return app;
        }

        public static CanvasDocument Create(string appName, string packagesPath, IList<string> paFiles, ErrorContainer errors)
        {
            var app = new CanvasDocument();

            app._properties = DocumentPropertiesJson.CreateDefault(appName);
            app._header = HeaderJson.CreateDefault();

            LoadTemplateFiles(app, packagesPath, out var loadedTemplates);
            app._entropy = new Entropy();
            app._checksum = new ChecksumJson() { ClientStampedChecksum = "Foo" };

            AddDefaultTheme(app);

            CreateControls(app, paFiles, loadedTemplates, errors);

            return app;
        }


        private static void LoadTemplateFiles(CanvasDocument app, string packagesPath, out Dictionary<string, ControlTemplate> loadedTemplates)
        {
            loadedTemplates = new Dictionary<string, ControlTemplate>();
            var templateList = new List<TemplatesJson.TemplateJson>();
            foreach (var file in new DirectoryReader(packagesPath).EnumerateFiles(string.Empty, "*.xml")) {
                var xmlContents = file.GetContents();
                if (!ControlTemplateParser.TryParseTemplate(xmlContents, app._properties.DocumentAppType, loadedTemplates, out var parsedTemplate, out var templateName))
                    throw new NotSupportedException($"Unable to parse template file {file._relativeName}");
                if (!file._relativeName.StartsWith(templateName))
                {
                    templateName = templateName.ToLower();
                }

                templateList.Add(new TemplatesJson.TemplateJson() { Name = templateName, Template = xmlContents, Version = parsedTemplate.Version });
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(loadedTemplates, app._properties.DocumentAppType);

            app._templates = new TemplatesJson() { UsedTemplates = templateList.ToArray() };
        }

        // The publish info points to the logo file. Grab it from the unknowns. 
        private static void GetLogoFileFromUnknowns(this CanvasDocument app)
        {
            // Logo file. 
            if (!string.IsNullOrEmpty(app._publishInfo?.LogoFileName))
            {
                string key = @"Resources\" + app._publishInfo.LogoFileName;
                FileEntry logoFile;
                if (app._unknownFiles.TryGetValue(key, out logoFile))
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
                if (file.Kind == FileKind.Templates)
                {
                    // Maybe we can recreate this from the template defaults instead?
                    foreach (var val in file.ToObject<Dictionary<string, CombinedTemplateState>>().Values)
                    {
                        app._templateStore.AddTemplate(val);
                    }
                    continue;
                }

                if (!file._relativeName.EndsWith(".editorstate.json"))
                {
                    errors.FormatNotSupported($"Unexpected file present in {EditorStateDir}");
                    throw new DocumentException();
                }

                // Json peer to a .pa file. 
                var controlExtraData = file.ToObject<Dictionary<string, ControlState>>();

                foreach (var control in controlExtraData)
                {
                    app._editorStateStore.TryAddControl(control.Value);
                }
            }

            foreach (var file in directory.EnumerateFiles(CodeDir, "*.pa.yaml"))
            {
                AddControl(app, file._relativeName, file.GetContents(), errors);
            }            
        }

        private static void CreateControls(CanvasDocument app, IList<string> paFiles, Dictionary<string, ControlTemplate> templateDefaults, ErrorContainer errors)
        {
            foreach (var file in paFiles)
            {
                var fileEntry = new DirectoryReader.Entry(file);

                AddControl(app, file, fileEntry.GetContents(), errors);
            }
        }

        private static void AddControl(CanvasDocument app, string filePath, string fileContents, ErrorContainer errors)
        {
            var filename = Path.GetFileName(filePath);
            try
            {
                var parser = new Parser.Parser(filePath, fileContents, errors);
                var controlIR = parser.ParseControl();
                app._sources.Add(controlIR.Name.Identifier, controlIR);
            }
            catch (DocumentException)
            {
                // On DocumentException, continue looking for errors in other files. 
            }
        }


        private static void LoadDataSources(CanvasDocument app, DirectoryReader directory)
        {
            // Will include subdirectories. 
            foreach (var file in directory.EnumerateFiles(DataSourcesDir, "*"))
            {
                var dataSource = file.ToObject<DataSourceEntry>();
                app.AddDataSourceForLoad(dataSource);                
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
            dir.DeleteAllSubdirs();

            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in app._templates.UsedTemplates)
            {
                var filename = $"{template.Name}_{template.Version}.xml";
                dir.WriteAllXML(PackagesDir, filename, template.Template);
                if (!ControlTemplateParser.TryParseTemplate(template.Template, app._properties.DocumentAppType, templateDefaults, out _, out _))
                    throw new NotSupportedException($"Unable to parse template file {template.Name}");
            }

            // Also add Screen and App templates (not xml, constructed in code on the server)
            GlobalTemplates.AddCodeOnlyTemplates(templateDefaults, app._properties.DocumentAppType);

            foreach (var control in app._sources)
            {
                var controlName = control.Key;
                var text = PAWriterVisitor.PrettyPrint(control.Value);

                string filename = controlName +".pa.yaml";
                dir.WriteAllText(CodeDir, filename, text);

                var extraData = new Dictionary<string, ControlState>();
                foreach (var item in app._editorStateStore.GetControlsWithTopParent(controlName))
                {
                    extraData.Add(item.Name, item);
                }

                // Write out of all the other state for roundtripping 
                string extraContent = controlName + ".editorstate.json";
                dir.WriteAllText(EditorStateDir, extraContent, JsonSerializer.Serialize(extraData, Utility._jsonOpts));
            }

            // Write out the used templates from controls
            // These could be created as part of build tooling, and are from the control.json files for now
            dir.WriteAllText(EditorStateDir, "ControlTemplates.json", JsonSerializer.Serialize(app._templateStore.Contents, Utility._jsonOpts));

            // Expansions....    
            // These are ignorable, but provide extra decoding and visiblity into complex files. 
            WriteIgnoreFiles(app, dir);

            // Data Sources  - write out each individual source. 
            HashSet<string> filenames = new HashSet<string>();
            foreach (var dataSource in app.GetDataSources())
            {
                // Filename doesn't actually matter, but careful to avoid collisions and overwriting. 
                // Also be determinstic. 
                string filename = dataSource.GetUniqueName()+ ".json";
                
                if (!filenames.Add(filename.ToLower()))
                {
                    int index = 1;
                    var altFileName = dataSource.GetUniqueName() + "_" + index + ".json";
                    while (!filenames.Add(altFileName.ToLower()))
                        ++index;

                    errors.GenericWarning("Data source name collision: " + filename + ", writing as " + altFileName + " to avoid.");
                    filename = altFileName;
                }

                dir.WriteAllJson(DataSourcesDir, filename, dataSource);
            }

            if (app._checksum != null)
            {
                dir.WriteAllJson(OtherDir, FileKind.Checksum, app._checksum);
            }

            if (app._logoFile != null)
            {
                dir.WriteAllBytes(OtherDir, app._logoFile.Name, app._logoFile.RawBytes);
            }

            if (app._themes != null)
            {
                dir.WriteAllJson(OtherDir, FileKind.Themes, app._themes);
            }

            // Loose files. 
            foreach (FileEntry file in app._unknownFiles.Values)
            {
                // Standardize the .json files so they're determinsitc and comparable
                if (file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
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

            dir.WriteAllJson(OtherDir, FileKind.Entropy, app._entropy);

            var manifest = new CanvasManifestJson
            {
                FormatVersion =  CurrentSourceVersion,
                Properties = app._properties,
                Header = app._header,
                PublishInfo = app._publishInfo
            };
            dir.WriteAllJson("", FileKind.CanvasManifest, manifest);

            if (app._connections != null)
            {
                dir.WriteAllJson(ConnectionDir, FileKind.Connections, app._connections);
            }
        }

        // Ignore these. but they help give more visibility into some of the json encoded fields.
        private static void WriteIgnoreFiles(this CanvasDocument app, DirectoryWriter directory)
        {
            foreach (var x in app.GetDataSources())
            {
                // DataEntityMetadataJson is a large json-encoded string for the IR. 
                if (x.DataEntityMetadataJson != null && x.DataEntityMetadataJson.Count > 0)
                {
                    foreach (var kv in x.DataEntityMetadataJson)
                    {
                        string filename = "DS_DataEntityMetadata_" + x.Name + "_" + kv.Key + ".json";
                        var jsonStr = kv.Value;
                        var je = JsonDocument.Parse(jsonStr).RootElement;

                        directory.WriteAllJson(Ignore, filename, je);
                    }
                }
                if (!string.IsNullOrEmpty(x.TableDefinition))
                {
                    // var path = Path.Combine(directory, "Ignore", "DS_TableDefinition_" + x.Name + ".json");
                    string filename = "DS_TableDefinition_" + x.Name + ".json";

                    var jsonStr = x.TableDefinition;
                    var je = JsonDocument.Parse(jsonStr).RootElement;
                    directory.WriteAllJson(Ignore, filename, je);
                }
            }

            // Dump DataComponentTemplates.json 


            // Properties. LocalConnectionReferences 
            directory.WriteDoubleEncodedJson(Ignore, "Properties_LocalDatabaseReferences.json", app._properties.LocalDatabaseReferences);
        }

        private static void AddDefaultTheme(CanvasDocument app)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(_defaultThemefileName);
            using var reader = new StreamReader(stream);

            var jsonString = reader.ReadToEnd();

            app._themes = JsonSerializer.Deserialize<ThemesJson>(jsonString, Utility._jsonOpts);
        }

    }
}
