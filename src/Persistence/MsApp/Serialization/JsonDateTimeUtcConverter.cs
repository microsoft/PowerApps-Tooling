// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;

/// <summary>
/// This converter forces DataTime values to UTC and serialized in UTC format.
/// <see cref="DateTimeFormatInfo.UniversalSortableDateTimePattern"/>.
/// </summary>
public class JsonDateTimeUtcConverter : JsonConverter<DateTime>
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
    /// <see cref="DateTimeFormatInfo.UniversalSortableDateTimePattern"/>.
    /// </summary>
    /// <returns>Date as string</returns>
    public static string WriteDate(DateTime date)
    {
        // See: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#UniversalSortable
        // DateTime must first be converted to UTC to ensure correct value output
        return date.ToUniversalTime().ToString("u");
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
