// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    // Define Schema.yaml - strong typing for input and output parameters
    public class ParameterSchema
    {
        public enum ParamType
        {
            Number,
            String,
            Boolean,
            Date,
            Time,
            DateTime,
            DateTimeNoTimeZone,
            Color,
            Guid,
            Record,
            Table,
            Blank,
            Hyperlink,
            OptionSetValue,
            UntypedObject,
            EntityRecord,
            EntityTable,
        }

        // Keep in sync with and eventually depend on Power-Fx/src/libraries/Microsoft.PowerFx.Core/Public/Types/Serialization/FormulaTypeSchema.cs
        public class Parameter
        {
            /// <summary>
            /// Represents the type of this item. For some complex types, additional optional data is required.
            /// </summary>
            public ParamType Type { get; set; }

            /// <summary>
            /// Optional. For Records and Tables, contains the list of fields.
            /// </summary>
            public Dictionary<string, Parameter> Fields { get; set; }

            /// <summary>
            /// Optional. Used for external schema definitions and input validation.
            /// </summary>
            public bool? Required { get; set; }

            /// <summary>
            /// Optional. For entities, specifies the table logical name.
            /// </summary>
            public string TableLogicalName { get; set; }

            /// <summary>
            /// Optional. For Option Set Values, specifies the option set logical name.
            /// </summary>
            public string OptionSetName { get; set; }
        }

        // Input parameters (public)
        public Dictionary<string, Parameter> Parameters { get; set; }

        // Output parameters (public)
        public Dictionary<string, Parameter> Outputs { get; set; }

        // Global variables within this page.  (internal)
        public Dictionary<string, Parameter> Globals { get; set; }
    }
}
