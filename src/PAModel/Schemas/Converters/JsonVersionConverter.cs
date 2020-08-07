//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Text.Json;

namespace Microsoft.AppMagic.Persistence.Converters
{
    /// <summary>
    /// Used to serialize and deserialize <see cref="Version"/>
    /// </summary>
    public class JsonVersionConverter : System.Text.Json.Serialization.JsonConverter<Version>
    {
        public override Version Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            Version.Parse(reader.GetString());

        public override void Write(
            Utf8JsonWriter writer,
            Version version,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(version.ToString());
    }
}
