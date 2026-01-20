// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Converters;

/// <summary>
/// This converter assumes all DataTime strings are in UTC format.
/// This is the format used by the document for DateTime strings saved in json files in an msapp.
/// </summary>
public class JsonDateTimeAssumesUtcConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
{
    // To prevent needing to have two converters (one for DateTime and one for Nullable<DateTime>)
    // we let the framework handle nulls for us.
    public override bool HandleNull => false;

    /// <summary>
    /// Assumes input to be universal and formatted according to
    /// <see cref="CultureInfo.InvariantCulture"/>, then parses the input as DateTime.
    /// manually specifies the kind as UTC and returns it.
    /// </summary>
    /// <param name="dateString">Date as string</param>
    /// <returns>Datetime value with UTC Kind</returns>
    public static DateTime ParseDate(string dateString)
    {
        return DateTime.Parse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            .ToUniversalTime();
    }

    /// <summary>
    /// Converts date to utc and returns it a string formatted according to
    /// <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <returns>Date as string</returns>
    public static string WriteDate(DateTime date)
    {
        // WARNING: even though the original converter says it converts the value to UTC, it doesn't; so we preserve that behavior.
        return date.ToString(CultureInfo.InvariantCulture);
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token type.");

        return ParseDate(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(WriteDate(value));
    }
}
