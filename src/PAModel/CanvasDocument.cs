// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// Represents a PowerApps document.  This can be save/loaded from a MsApp or Source representation. 
    /// This is a full in-memory representation of the msapp file. 
    /// </summary>
    public class CanvasDocument
    {
        // Rules for CanvasDocument
        // - Save/Load must faithfully roundtrip an msapp exactly. 
        // - this is an in-memory representation - so it must parse/shard everything on load. 
        // - Save should not mutate any state. 

        // Track all unknown "files". Ensures round-tripping isn't lossy.         
        // Only contains files of FileKind.Unknown
        internal Dictionary<string, FileEntry> _unknownFiles = new Dictionary<string, FileEntry>();

        // Key is Top Parent Control Name.
        // Includes both Controls and Components. 
        internal Dictionary<string, SourceFile> _sources = new Dictionary<string, SourceFile>();

        // Various data sources        
        // This is references\dataSources.json
        // Also includes entries for DataSources made from a DataComponent
        // private Dictionary<string, DataSourceEntry> _dataSources = new Dictionary<string, DataSourceEntry>();
        // List instead of Dict  since we don't have a unique key. Name can be reused. 
        private List<DataSourceEntry> _dataSources = new List<DataSourceEntry>();

        internal HeaderJson _header;
        internal DocumentPropertiesJson _properties;
        internal PublishInfoJson _publishInfo;
        internal TemplatesJson _templates;
        internal ThemesJson _themes;

        // Environment-specific information
        // Extracted from _properties.LocalConnectionReferences
        // Key is a Connection.Id
        internal IDictionary<string, ConnectionJson> _connections;

        internal FileEntry _logoFile;

        // Save for roundtripping.
        internal Entropy _entropy = new Entropy();

        // Information about data components. 
        // TemplateGuid --> Info
        internal Dictionary<string, MinDataComponentManifest> _dataComponents = new Dictionary<string, MinDataComponentManifest>();
        
        // checksum from existin msapp. 
        internal ChecksumJson _checksum;

        #region Save/Load 
        public static CanvasDocument LoadFromMsapp(string fullPathToMsApp)
        {
            return MsAppSerializer.Load(fullPathToMsApp);
        }
        public static CanvasDocument LoadFromSources(string pathToSourceDirectory)
        {
            return SourceSerializer.LoadFromSource(pathToSourceDirectory);
        }
        public void SaveToMsApp(string fullPathToMsApp)
        {
            MsAppSerializer.SaveAsMsApp(this, fullPathToMsApp);
        }
        public void SaveToSources(string pathToSourceDirectory)
        {
            SourceSerializer.SaveAsSource(this, pathToSourceDirectory);
        }
        public static CanvasDocument MakeFromSources(string appName, string packagesPath, IList<string> paFiles)
        {
            return SourceSerializer.Create(appName, packagesPath, paFiles);
        }
        #endregion


        // iOrder is used to preserve ordering value for round-tripping. 
        internal void AddDataSourceForLoad(DataSourceEntry ds, int? order = null)
        {
            // Don't allow overlaps;
            // Names are not unique. 
            _dataSources.Add(ds);

            this._entropy.Add(ds, order);            
        }
        internal IEnumerable<DataSourceEntry> GetDataSources()
        {
            return _dataSources;
        }

        // Called after loading. This will check internal fields and fill in consistency data. 
        internal void OnLoadComplete()
        {
            // Do integrity checks. 
            if (this._header == null)
            {
                throw new InvalidOperationException($"Missing header file");
            }
            if (this._properties == null)
            {
                throw new InvalidOperationException($"Missing properties file");
            }


            // Associate a data component with its sources. 
            foreach (var kv in this._sources.Values)
            {
                if (kv.Kind == SourceKind.DataComponent || kv.Kind == SourceKind.UxComponent)
                {
                    MinDataComponentManifest dc = this._dataComponents[kv.TemplateName];
                    dc._sources = kv.Value;
                }
            }

            // Integrity checks. 
            // Make sure every connection has a corresponding data source. 
            foreach (var kv in this._connections.NullOk())
            {
                var connection = kv.Value;

                if (kv.Key != connection.id)
                {
                    throw new InvalidOperationException($"Document consistency error. Id mismatch");
                }
                foreach(var dataSourceName in connection.dataSources)
                {
                    var ds = this._dataSources.Where(x => x.Name == dataSourceName).FirstOrDefault();
                    if (ds == null)
                    {
                        throw new InvalidOperationException($"Document error: Connection '{dataSourceName}' does not have a corresponding data source.");
                    }
                }
            }            
        }

        // $$$ Update a datasource? 
        // Chevron scenario. 
        internal void UpdateDataSource(DataSourceEntry dataSource)
        {
            DataSourceEntry existing = _dataSources.Where(x => x.Name == dataSource.Name).FirstOrDefault();
            if (existing == null)
            {
                throw new NotSupportedException($"Can't add a new data source '{dataSource.Name}'. Just update existing.");
            }

            if (existing.ApiId != dataSource.ApiId)
            {
                throw new NotSupportedException($"Can't change data source type from {existing.ApiId} to {dataSource.ApiId}");
            }

            if (existing.TableName == dataSource.TableName)
            {
                // Same. nop
                return; 
            }

            // Mutate 
            // - References\DataSource.json 
            //    - entry
            //    - DataEntityMetadataJson
            // - Properties.json
            //    - LocalConnectionReferences

            foreach(DataSourceEntry x in this.GetDataSources())
            {
                if (x.Name == dataSource.Name)
                {
                    UpdateMetadata(x, dataSource);                                        
                    break;
                }
            }                                   
        }

        // Update oldDataSource in place to match the new datasource. 
        // Caller has checked these Sources are compatible. 
        // Also make corresponding updates in properties.json
        private void UpdateMetadata(DataSourceEntry oldDataSource, DataSourceEntry dataSource)
        {
            // Replace the guidl. 
            string oldGuid = oldDataSource.TableName;
            string newGuid = dataSource.TableName;

            // $$$ Is string replace too broad? May break other usages?
            string oldName = oldDataSource.GetSharepointListName();
            string newName = dataSource.GetSharepointListName();

            var currentMetadataDict = oldDataSource.DataEntityMetadataJson;
            string currentMetadata = currentMetadataDict[oldGuid];
            string newMetadata = currentMetadata.Replace(oldGuid, newGuid).Replace(oldName, newName);

            oldDataSource.DataEntityMetadataJson = new Dictionary<string, string> {
                { newGuid,  newMetadata }
             };
            oldDataSource.DatasetName = dataSource.DatasetName;
            oldDataSource.TableName = dataSource.TableName;

            // Hack: LocalConnectionReferences.
            this._properties.LocalConnectionReferences =
                this._properties.LocalConnectionReferences.
                Replace(oldGuid, newGuid).Replace(oldName, newName);

        }


        // $$$
        internal void CheckForUpdates()
        {
            // If we've changed a DataSource, do we need to push updates back  into the other files? 


        }

                

        // https://github.com/Microsoft/powerapps-tools
        // https://github.com/microsoft/powerapps-tools/tree/master/Tools/Apps/Microsoft.PowerApps.Tools.PhoneAppConverter
        // https://github.com/microsoft/powerapps-tools/blob/master/Tools/Core/Microsoft.PowerApps.Tools.PhoneToTablet/Converter.cs
        public void UpdateLayout()
        {

            // Here's the converter they apply 
#if false
            string popertiesFile = Path.Combine(tempPath, fileName, "Properties.json");
            Microsoft.PowerApps.Tools.AppEntities.PropertyModel prop = JsonConvert.DeserializeObject<Microsoft.PowerApps.Tools.AppEntities.PropertyModel>(File.ReadAllText(popertiesFile));

            prop.DocumentLayoutWidth = 1366.0f;
            prop.DocumentLayoutHeight = 768.0f;
            prop.DocumentLayoutOrientation = "landscape";
            prop.DocumentAppType = "DesktopOrTablet";
#endif
        }

        // Tempalte is the guid. 
        // Throw on missing. 
        internal MinDataComponentManifest LookupDCByTemplateName(string dataComponentTemplate)
        {
            return (from x in this._dataComponents.Values
                    where x.TemplateGuid == dataComponentTemplate
                    select x).First();
        }

        // Find the controlId for the dataComponent instance of this particular template. 
        internal IEnumerable<string> LookupControlIdsByTemplateName(string templateGuid)
        {
            foreach(var source in this._sources.Values)
            {
                ControlInfoJson controlJson = source.Value;

                foreach (var child in controlJson.TopParent.Children)
                {
                    if (child.Template.Name == templateGuid)
                    {
                        yield return child.ControlUniqueId;
                    }
                }

                    /*
                    var all = WalkAll(controlJson.TopParent);
                    foreach(ControlInfoJson.Item item in all)
                    {
                        if (item.Template.Name == templateGuid)
                        {
                            yield return item.ControlUniqueId;
                        }
                    }*/
                }
        }

        internal static IEnumerable<ControlInfoJson.Item> WalkAll(ControlInfoJson.Item x)
        {
            yield return x;
            if (x.Children != null)
            {
                foreach(var child in x.Children)
                {
                    var subItems = WalkAll(child);
                    foreach (var subItem in subItems)
                    {
                        yield return subItem;
                    }
                }
            }
        }
    }    
}
