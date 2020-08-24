//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Text.Json;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    public enum DataComponentDependencyKind
    {
        Cds,
        DataComponent,
        Unknown
    }

    public enum DataComponentDefinitionKind
    {
        /// <summary>
        /// Data component type mirrors a CDS entity.
        /// </summary>
        Extension,

        /// <summary>
        /// Component has its own type. e.g. combination of one or more entities
        /// </summary>
        Composition,

        /// <summary>
        /// Not yet defined.
        /// </summary>
        Unknown
    }

    internal class CdsDataDependencyJson
    {
        public string LogicalName { get; set; }
        public string DataSetName { get; set; }
    }

    internal class DataComponentDependencyJson
    {
        public string DataComponentTemplateName { get; set; }
    }

    internal class DataComponentDataDependencyJson
    {
        public DataComponentDependencyKind DataComponentExternalDependencyKind { get; set; }
        public CdsDataDependencyJson DataComponentCdsDependency { get; set; }
        public DataComponentDependencyJson DataComponentDependency { get; set; }
    }

    /// <summary>
    /// Schematic class which represents a data component definition, i.e. names and template
    /// </summary>
    internal class DataComponentDefinitionJson
    {
        public string PreferredName { get; set; }
        public string LogicalName { get; set; }
        public string DependentEntityName { get; set; }
        public string ComponentRawMetadataKey { get; set; }
        public string ControlUniqueId { get; set; }
        public DataComponentDefinitionKind DataComponentKind { get; set; }
        public DataComponentDataDependencyJson[] DataComponentExternalDependencies { get; set; }
    }


    internal class ComponentDefinitionInfoJson
    {
        public string Name { get; set; }
        public string LastModifiedTimestamp { get; set; } //  "637335420246436668",
        public ControlInfoJson.RuleEntry[] Rules {get;set;}

        public JsonElement ControlPropertyState { get; set; }
        public JsonElement Children { get; set; }
    }
}