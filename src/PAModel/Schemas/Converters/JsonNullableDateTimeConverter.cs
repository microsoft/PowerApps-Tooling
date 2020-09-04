// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.AppMagic.Persistence.Converters
{
    public class JsonNullableDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime?>
    {
        public override DateTime? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            var maybeDate = reader.GetString();

            if (string.IsNullOrEmpty(maybeDate))
                return null;

            return JsonDateTimeConverter.ParseDate(maybeDate);
        }


        public override void Write(
            Utf8JsonWriter writer,
            DateTime? dateTimeValue,
            JsonSerializerOptions options)
        {
            if (dateTimeValue.HasValue)
                writer.WriteStringValue(JsonDateTimeConverter.WriteDate(dateTimeValue.Value));
        }
    }
}