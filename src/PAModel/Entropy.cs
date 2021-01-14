// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Various data that we can save for round-tripping.
    // Everything here is optional!!
    // Only be written during MsApp. Opaque for source file.
    internal class Entropy
    {
        // These come from volatile properties in properties.json in the msapp
        internal class PropertyEntropy
        {
            public string LocalDatabaseReferences { get; set; }
            public Dictionary<string, int> ControlCount { get; set; }
            public double? DeserializationLoadTime { get; set; }
            public double? AnalysisLoadTime { get; set; }
        }

        // Json serialize these. 
        public Dictionary<string, string> TemplateVersions { get; set; }  = new Dictionary<string, string>(StringComparer.Ordinal);
        public DateTime? HeaderLastSavedDateTimeUTC { get; set; }
        public string OldLogoFileName { get; set; }

        // This is a utf8 string representing the AppCheckerResult.sarif file
        // It likely is double-encoded json.
        public string AppCheckerResult { get; set; }

        // To fully round-trip, we need to preserve array order for the various un-ordered arrays that we may split apart.         
        public Dictionary<string, int> OrderDataSource { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OrderComponentMetadata { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OrderTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OrderXMLTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OrderComponentTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        // Key is component name, value is Index. 
        public Dictionary<string, double> ComponentIndexes { get; set; } = new Dictionary<string, double>(StringComparer.Ordinal);

        // Key is top parent, value is Index offset
        public Dictionary<string, double> PublishOrderIndexOffsets { get; set; } = new Dictionary<string, double>(StringComparer.Ordinal);

        // Key is control name, value is uniqueId
        public Dictionary<string, int> ControlUniqueIds { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public PropertyEntropy VolatileProperties { get; set; }

        public int GetOrder(DataSourceEntry dataSource)
        {
            return this.OrderDataSource.GetOrDefault<string,int>(dataSource.GetUniqueName(), -1);
        }
        public void Add(DataSourceEntry entry, int? order)
        {
            if (order.HasValue)
            {
                this.OrderDataSource[entry.GetUniqueName()] = order.Value;
            }
        }

        public int GetOrder(ComponentsMetadataJson.Entry entry)
        {
            return this.OrderComponentMetadata.GetOrDefault<string, int>(entry.TemplateName, -1);
        }
        public void Add(ComponentsMetadataJson.Entry entry, int order)
        {
            this.OrderComponentMetadata[entry.TemplateName] = order;
        }

        public int GetOrder(TemplateMetadataJson entry)
        {
            return this.OrderTemplate.GetOrDefault<string, int>(entry.Name, -1);
        }
        public void Add(TemplateMetadataJson entry, int order)
        {
            this.OrderTemplate[entry.Name] = order;
        }

        public int GetOrder(TemplatesJson.TemplateJson entry)
        {
            return this.OrderXMLTemplate.GetOrDefault<string, int>(entry.Name, -1);
        }
        public void Add(TemplatesJson.TemplateJson entry, int order)
        {
            this.OrderXMLTemplate[entry.Name] = order;
        }

        public int GetComponentOrder(TemplateMetadataJson entry)
        {
            return this.OrderComponentTemplate.GetOrDefault<string, int>(entry.Name, -1);
        }
        public void AddComponent(TemplateMetadataJson entry, int order)
        {
            this.OrderComponentTemplate[entry.Name] = order;
        }

        public void SetHeaderLastSaved(DateTime? x)
        {
            this.HeaderLastSavedDateTimeUTC = x;            
        }
        public DateTime? GetHeaderLastSaved()
        {
            return this.HeaderLastSavedDateTimeUTC;
        }

        public void SetTemplateVersion(string dataComponentGuid, string version)
        {
            TemplateVersions[dataComponentGuid] = version;
        }

        public string GetTemplateVersion(string dataComponentGuid)
        {
            string version;
            TemplateVersions.TryGetValue(dataComponentGuid, out version);

            // Version string is ok to be null. 
            // DateTime.Now.ToUniversalTime().Ticks.ToString();
            return version;
        }

        public void SetLogoFileName(string oldLogoName)
        {
            this.OldLogoFileName = oldLogoName;
        }

        public void SetProperties(DocumentPropertiesJson documentProperties)
        {
            VolatileProperties = new PropertyEntropy()
            {
                AnalysisLoadTime = documentProperties.AnalysisLoadTime,
                DeserializationLoadTime = documentProperties.DeserializationLoadTime,
                ControlCount = documentProperties.ControlCount,
                LocalDatabaseReferences = documentProperties.LocalDatabaseReferences
            };

            documentProperties.AnalysisLoadTime = null;
            documentProperties.DeserializationLoadTime = null;
            documentProperties.ControlCount = null;
            documentProperties.LocalDatabaseReferences = null;
        }

        public void GetProperties(DocumentPropertiesJson documentProperties)
        {
            if (this.VolatileProperties != null)
            {
                documentProperties.AnalysisLoadTime = VolatileProperties.AnalysisLoadTime;
                documentProperties.DeserializationLoadTime = VolatileProperties.DeserializationLoadTime;
                documentProperties.ControlCount = VolatileProperties.ControlCount;
                documentProperties.LocalDatabaseReferences = VolatileProperties.LocalDatabaseReferences;
            }
        }
    }
}
