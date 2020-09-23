// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define USEPA

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Read/Write to a source format. 
    internal static partial class SourceSerializer
    {
        public static Version CurrentSourceVersion = new Version(0, 1);

        // Layout is:
        //  src\
        //  DataSources\
        //  Other\  (all unrecognized files)         
        public const string CodeDir = "Src";
        public const string TemplatesDir = "pkgs";
        public const string OtherDir = "Other"; // exactly match files from .msapp format
        public const string ConnectionDir = "Connections";
        public const string DataSourcesDir = "DataSources";
        public const string Ignore = "Ignore"; // Write-only, ignore these files.

        private static T ToObject<T>(string fullpath)
        {
            var str = File.ReadAllText(fullpath);
            return JsonSerializer.Deserialize<T>(str, Utility._jsonOpts);
        }


        // Full fidelity read-write

        public static CanvasDocument LoadFromSource(string directory2)
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
                            throw new NotSupportedException($"This tool only supports {CurrentSourceVersion}, the manifest version is {manifest.FormatVersion}");
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
                throw new NotSupportedException($"Can't find CanvasManifest.json file - is sources an old version?");
            }

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
            LoadSourceFiles(app, dir);

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

        // The publish info points to the logo file. Grab it from the unknowns. 
        private static void GetLogoFileFromUnknowns(this CanvasDocument app)
        {
            // Logo file. 
            if (!string.IsNullOrEmpty(app._publishInfo.LogoFileName))
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

        private static void LoadSourceFiles(CanvasDocument app, DirectoryReader directory)
        {
            // Ignoring real pa1 files, can't parse them yet. 
            // Sources
            var templates = new Dictionary<string, ControlInfoJson.Template>();
            var controlData = new Dictionary<string, Dictionary<string, ControlInfoJson.Item>>();

            foreach (var file in directory.EnumerateFiles(CodeDir, "*.json"))
            {
                if (file.Kind == FileKind.CanvasManifest)
                {
                    continue;
                }

                if (file.Kind == FileKind.Templates)
                {
                    templates = file.ToObject<Dictionary<string, ControlInfoJson.Template>>();
                    continue;
                }

                bool isDataComponentManifest = file._relativeName.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase);
                if (isDataComponentManifest)
                {
                    var json = file.ToObject< MinDataComponentManifest>();
                    app._dataComponents.Add(json.TemplateGuid, json);
                }
                else if (file._relativeName.EndsWith(".editorstate.json", StringComparison.OrdinalIgnoreCase))
                {
#if USEPA
                    // Json peer to a .pa file. 
                    var controlExtraData = file.ToObject<Dictionary<string, ControlInfoJson.Item>>();
                    var filename = Path.GetFileName(file._relativeName);
                    var controlName = filename.Remove(filename.IndexOf(".editorstate.json"));

                    controlData.Add(controlName, controlExtraData);
#endif
                } 
                else
                {
#if !USEPA
                    // Eventually, get rid of the json and do everything from .pa          
                    var control = file.ToObject<ControlInfoJson>();

                    var sf = SourceFile.New(control);

                    // If a source file already exists, check the source directory for duplicate filenames.
                    // Could be multiple that escape to the same value. 
                    app._sources.Add(sf.ControlName, sf);
#endif
                }
            }

#if USEPA
            foreach (var file in directory.EnumerateFiles(CodeDir, "*.pa1"))
            {
                var filename = Path.GetFileName(file._relativeName);
                var controlName = filename.Remove(filename.IndexOf(".pa1"));
                if (!controlData.TryGetValue(controlName, out var controlState))
                    throw new NotImplementedException("Missing control state json, reconstructing not yet supported");

                try
                {
                    var parser = new Microsoft.PowerPlatform.Formulas.Tools.Parser.Parser(file._relativeName, file.GetContents(), controlState, templates);                        
                    var item = parser.ParseControl();
                    if (parser.HasErrors())
                    {
                        parser.WriteErrors();
                        Console.WriteLine("Skipping adding file to .msapp due to parse errors");
                        Console.WriteLine("This tool is still in development, if these errors are wrong, please open an issue on our github page with a copy of your app");

                        continue;
                    }


                    var control = new ControlInfoJson() { TopParent = item };

                    var sf = SourceFile.New(control);

                    // If a source file already exists, check the source directory for duplicate filenames.
                    // Could be multiple that escape to the same value. 
                    app._sources.Add(sf.ControlName, sf);
                }
                catch
                {
                    Console.WriteLine(
                        "Parsing failed for file " + filename + "\n" +
                        "This tool is still in development, please open an issue on our github page with a copy of your app");
                }

            }
#endif
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
        public static void SaveAsSource(this CanvasDocument app, string directory2)
        { 
            var dir = new DirectoryWriter(directory2);
            dir.DeleteAllSubdirs();

            // Shard templates, parse for default values
            var templateDefaults = new Dictionary<string, ControlTemplate>();
            foreach (var template in app._templates.UsedTemplates)
            {
                var filename = $"{template.Name}_{template.Version}.xml";
                dir.WriteAllText(TemplatesDir, filename, template.Template);
                if (ControlTemplateParser.TryParseTemplate(template.Template, template.Name, app._properties.DocumentAppType, out var parsedTemplate))
                    templateDefaults.Add(template.Name, parsedTemplate);
            }


            var templates = new Dictionary<string, ControlInfoJson.Template>();

            foreach (var control in app._sources.Values)
            {
                // Temporary write out of JSON for roundtripping
#if !USEPA
                string jsonContentFile = control.ControlName + ".json";
                dir.WriteAllText(CodeDir, jsonContentFile, JsonSerializer.Serialize(control.Value, Utility._jsonOpts));
#endif

                var text = PAConverter.GetPAText(control, templateDefaults);
                var controlName = control.ControlName;
                string filename = controlName +".pa1";
                dir.WriteAllText(CodeDir, filename, text);

                var extraData = new Dictionary<string, ControlInfoJson.Item>();
                foreach (var item in control.Flatten().ToList())
                {
                    var name = item.Name;
                    if (!templates.ContainsKey(item.Template.Name))
                    {
                        templates.Add(item.Template.Name, item.Template);
                    }
                    item.Name = null;
                    item.Parent = null;
                    item.Template = null;
                    foreach (var rule in item.Rules)
                    {
                        rule.InvariantScript = null;
                    }
                    item.Children = null;

                    extraData.Add(name, item);
                }

                // Write out of all the other state for roundtripping 
                string extraContent = controlName + ".editorstate.json";
                dir.WriteAllText(CodeDir, extraContent, JsonSerializer.Serialize(extraData, Utility._jsonOpts));
            }

            // Write out the used templates from controls
            // These could be created as part of build tooling, and are from the control.json files for now
            dir.WriteAllText(CodeDir, "ControlTemplates.json", JsonSerializer.Serialize(templates, Utility._jsonOpts));

            // Write out DataComponent pieces.
            // These could all be infered from the .pa file, so write next to the src. 
            foreach (MinDataComponentManifest dataComponent in app._dataComponents.Values)
            {
                string controlName = dataComponent.Name;
                dir.WriteAllJson(CodeDir, controlName + ".manifest.json", dataComponent);
            }

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
                
                if (!filenames.Add(filename))
                {
                    // Danger - overwriting file! 
                    throw new NotImplementedException($"duplicate - overwriting {filename}");
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
    }
}
