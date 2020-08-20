using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PAModel
{
    // Must be flexible about what files we see in the .msapp
    // In-memory representation for the app model (not the same as on-disk representation)
    public class MsApp
    {
        // Track all unknown "files". Ensures round-tripping isn't lossy.         
        // Only contains files of FileKind.Unknown
        internal Dictionary<string, FileEntry> _unknownFiles = new Dictionary<string, FileEntry>();

        // Key is Control Name.
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


        // Save for roundtripping.
        internal Entropy _entropy = new Entropy();

        // Map of String-->Guid for DataComponents.
        internal Dictionary<string, MinDataComponentManifest> _dataComponents = new Dictionary<string, MinDataComponentManifest>();

        internal void AddDataSourceForLoad(DataSourceEntry ds)
        {
            // Don't allow overlaps;
            // Names are not unique. 
            _dataSources.Add(ds);
        }
        internal IEnumerable<DataSourceEntry> GetDataSources()
        {
            return _dataSources;
        }

        // Called after loading. This will check internal fields and fill in consistency data. 
        internal void OnLoadComplete()
        {
            // Do integrity checks. 
            // PopulateDataSourcesFromRawFile();
        }

        // $$$ Update a datasource? 
        // Chevron scenario. 
        public void UpdateDataSource(DataSourceEntry dataSource)
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
