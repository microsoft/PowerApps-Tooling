using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace PAModel
{
    // Read/Write to a source format. 
    public static partial class SourceSerializer
    {
        // Layout is:
        //  src\
        //  DataSources\
        //  Other\  (all unrecognized files)         
        public const string CodeDir = "Src";
        public const string OtherDir = "Other"; // exactly match files from .msapp format
        public const string DataSourcesDir = "DataSources";
        public const string Ignore = "Ignore"; // Write-only, ignore these files.

        private static T ToObject<T>(string fullpath)
        {
            var str = File.ReadAllText(fullpath);
            return JsonSerializer.Deserialize<T>(str, Utility._jsonOpts);
        }


        // Full fidelity read-write

        public static MsApp LoadFromSource(string directory2)
        {
            var dir = new DirectoryReader(directory2);
            
            // $$$ Duplicate with MsAppSerializer? 
            var app = new MsApp();

            // var root = Path.Combine(directory, OtherDir);
            foreach (var file in dir.EnumerateFiles(OtherDir))
            {
                // Special files like Header / Properties 
                //var relativeName = Utility.GetRelativePath(fullPath, root);
                //var kind = FileEntry.TriageKind(relativeName);
                switch (file.Kind)
                {
                    default:
                        // Track any unrecognized files so we can save back.
                        app.AddFile(file.ToFileEntry());
                        break;

                    case FileKind.Properties:
                        app._properties = file.ToObject<DocumentPropertiesJson>();
                        break;

                    case FileKind.Header:
                        app._header = file.ToObject<HeaderJson>();
                        break;

                    case FileKind.ComponentSrc:
                    case FileKind.ControlSrc:
                        {
                            // !!! Shouldn't find any here -  were explicit in source
                            throw new InvalidOperationException($"Unexpected source file: " + file._fullpath);
                            //var control = ToObject<ControlInfoJson>(fullPath);
                            //var sf = SourceFile.New(control);
                            //app._sources.Add(sf.ControlName, sf);
                        }
                        break;
                }
            } // each loose file in '\other' 

            LoadDataSources(app, dir);
            LoadSourceFiles(app, dir);

            app.OnLoadComplete();

            return app;
        }

        private static void LoadSourceFiles(MsApp app, DirectoryReader directory)
        {
            // Sources
            foreach (var file in directory.EnumerateFiles(CodeDir, "*.pa*"))
            {
                var sf = PAConverter.ReadSource(file._fullpath);
                app._sources.Add(sf.ControlName, sf);
            }

            // Extra metadata files for data components 
            foreach (var file in directory.EnumerateFiles(CodeDir, "*.json"))
            {
                var json = file.ToObject<MsApp.DataComponentInfo>();
                app._dataComponents.Add(json.TemplateGuid, json);
            }
        }

        private static void LoadDataSources(MsApp app, DirectoryReader directory)
        {
            foreach (var file in directory.EnumerateFiles(DataSourcesDir, "*"))
            {
                var dataSource = file.ToObject<DataSourceEntry>();
                app._dataSources[dataSource.Name] = dataSource;
            }
        }

        // Write out to a directory (this shards it) 
        public static void SaveAsSource(this MsApp app, string directory2)
        {
            var dir = new DirectoryWriter(directory2);

            foreach (var control in app._sources.Values)
            {                
                var text = PAConverter.GetPAText(control);

                string filename = control.ControlName +".pa1";
                dir.WriteAllText(CodeDir, filename, text);

                // SourceFormat assumed to include everything. 
                // $$$ Split out into view state? 
                // Write out raw JSON for things that can't be PA
                // WriteFile(control.ToMsAppFile(), directory);
            }

            // Write out DataComponent pieces.
            // These could all be infered from the .pa file, so write next to the src. 
            foreach(var dataComponent in app._dataComponents.Values)
            {
                string controlName = dataComponent.Name;
                dir.WriteAllJson(CodeDir, controlName + "_dc.json", dataComponent);
            }

            // Expansions....    
            // These are ignorable, but provide extra decoding and visiblity into complex files. 
            WriteIgnoreFiles(app, dir);

            // Data Sources  - write out each individual source. 
            foreach (var dataSource in app._dataSources.Values)
            {
                string filename = dataSource.Name + ".json";
                dir.WriteAllJson(DataSourcesDir, filename, dataSource);
            }

            // Loose files. 
            foreach (FileEntry file in app._unknownFiles.Values)
            {
                // Standardize the .json files so they're determinsitc and comparable
                if (file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // $$$ JsonWriter still not deterministic. 
                    ReadOnlyMemory<byte> span = file.RawBytes;
                    var je = JsonDocument.Parse(span).RootElement;

                    // $$$ Don't mutate. 
                    var bytes = je.ToBytes();
                    file.RawBytes = bytes;
                }
                dir.WriteAllBytes(OtherDir, file.Name, file.RawBytes);
            }
                        
            dir.WriteAllJson(OtherDir, FileKind.Header, app._header);
            dir.WriteAllJson(OtherDir, FileKind.Properties, app._properties);

        }

        // Ignore these. but they help give more visibility into some of the json encoded fields.
        private static void WriteIgnoreFiles(this MsApp app, DirectoryWriter directory)
        {
            foreach (var x in app._dataSources.Values)
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
            directory.WriteDoubleEncodedJson(Ignore, "Properties_LocalConnectionReferences.json", app._properties.LocalConnectionReferences);
            directory.WriteDoubleEncodedJson(Ignore, "Properties_LocalDatabaseReferences.json", app._properties.LocalDatabaseReferences);
        }   
    }
}
