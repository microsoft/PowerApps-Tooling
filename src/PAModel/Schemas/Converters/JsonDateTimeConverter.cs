// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.AppMagic.Persistence.Converters
{
    public class JsonDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        /// <summary>
        /// Assumes input to be universal and formatted according to
        /// <see cref="CultureInfo.InvariantCulture"/>, then parses the input as DateTime 
        /// and returns it.
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        public static DateTime ParseDate(string dateString) => DateTime.Parse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        /// <summary>
        /// Converts date to utc and returns it a string formatted according to
        /// <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <returns>Date as string</returns>
        public static string WriteDate(DateTime date) => date.ToUniversalTime().ToString(CultureInfo.InvariantCulture);

        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => ParseDate(reader.GetString());

        public override void Write(
            Utf8JsonWriter writer,
            DateTime dateTimeValue,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(WriteDate(dateTimeValue));
    }
}
