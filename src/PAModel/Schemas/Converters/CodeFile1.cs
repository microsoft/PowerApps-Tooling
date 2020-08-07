//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AppMagic.Persistence.Converters
{
    public class JsonPropertyOrderConverter: JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Abstract, can't fall through to base. 
            return JsonSerializer.Deserialize(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // Special case ExtensionData 
            // Dictionary<string, > d = new Dictionary<string, JsonElement>(); 
            
            var t = value.GetType();
            
            foreach(var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                // d[prop.Name] = 
            }

            // JsonSerializer.Serialize(writer, Vector2,  )
        }
    }
}