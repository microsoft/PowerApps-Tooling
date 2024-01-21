// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools.JsonConverters;

/// <summary>
/// Used to serialize and deserialize <see cref="Version"/>
/// </summary>
internal class JsonVersionConverter : System.Text.Json.Serialization.JsonConverter<Version>
{
    public override Version Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return Version.Parse(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        Version version,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(version.ToString());
    }
}
