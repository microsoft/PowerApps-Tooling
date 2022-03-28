// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    /// <summary>
    /// Contains property state for AutoLayout properties not written to .pa
    /// <see cref="ControlInfoJson.DynamicPropertyJson"/>
    /// </summary>
    internal class DynamicPropertyState
    {
        public string PropertyName { get; set; }

        public PropertyState Property { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
