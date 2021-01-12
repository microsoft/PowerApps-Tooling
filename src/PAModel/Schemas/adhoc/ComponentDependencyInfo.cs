using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    /// <summary>
    /// Describes Properties.LibraryDependencies, which is an ordered json array of these.
    /// Each item means a component was downloaded from a library. 
    /// </summary>
    public class ComponentDependencyInfo
    {
        // OriginalComponentDefinitionTemplateId - specifies temmplate id. 

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
