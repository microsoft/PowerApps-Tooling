// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    internal class ResourcesJson
    {
        public ResourceJson[] Resources { get; set; }
    }

    internal class ResourceJson
    {
        public string Name { get; set; }
        public string ResourceKind { get; set; }
        public string RootPath { get; set; }        

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
