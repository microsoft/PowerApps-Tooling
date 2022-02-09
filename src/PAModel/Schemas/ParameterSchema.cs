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
            Color,
            Guid,
            Record,
            Table,
            EntityRecord,
            EntityTable,
        }

        public class Parameter
        {
            public ParamType Type { get; set; }
            public bool Required { get; set; }

            // Optional: For entities, specifies the name
            public string Name { get; set; }

            // Optional: For records, can become recursive
            public Dictionary<string, Parameter> Fields { get; set; }
        }
                
        // Input parameters (public)
        public Dictionary<string, Parameter> Parameters { get; set; }

        // Output parameters (public)
        public Dictionary<string, Parameter> Outputs { get; set; }

        // Global variables within this page.  (internal)
        public Dictionary<string, Parameter> Globals { get; set; }
    }
}
