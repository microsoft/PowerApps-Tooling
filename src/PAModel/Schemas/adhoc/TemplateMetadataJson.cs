// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    // From PowerApps-Client\src\Cloud\DocumentServer.Core\Document\Document\Persistence\Serialization\Schemas\Control\Template\TemplateMetadataJson.cs
    internal class TemplateMetadataJson
    {
        public string Name { get; set; }
        public string OriginalName { get; set; }

        // Ok to be null. 
        //  Will default to: DateTime.Now.ToUniversalTime().Ticks.ToString();
        public string Version { get; set; }
        public ComponentType? ComponentType { get; set; } = null;

        public bool? IsComponentLocked { get; set; }
        public bool? ComponentChangedSinceFileImport { get; set; }
        public bool? ComponentAllowCustomization { get; set; }

        public CustomPropertyJson[] CustomProperties { get; set; }

        public DataComponentDefinitionJson DataComponentDefinitionKey { get; set; } = null;
        public void Validate()
        {
            if (DataComponentDefinitionKey?.ComponentRawMetadataKey != null)
            {
                throw new NotSupportedException("Does not support older formats using ComponentRawMetadataKey");
            }
        }
    }
}
