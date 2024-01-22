// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.PowerPlatform.Formulas.Tools.JsonConverters;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Extensions;

public static class JsonExtensions
{
    private static JsonSerializerOptions GetJsonOptions()
    {
        var opts = new JsonSerializerOptions
        {
            // encodes quote as \" rather than unicode.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        opts.Converters.Add(new JsonStringEnumConverter());

        opts.Converters.Add(new JsonDateTimeConverter());
        opts.Converters.Add(new JsonVersionConverter());

        opts.WriteIndented = true;
        opts.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        return opts;
    }

    public static JsonSerializerOptions _jsonOpts = GetJsonOptions();

    // https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround

    public static string JsonSerialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _jsonOpts);
    }

    public static T JsonParse<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonOpts);
    }


    public static T ToObject<T>(this JsonElement element)
    {
        var json = element.GetRawText();
        return JsonSerializer.Deserialize<T>(json, _jsonOpts);
    }
    public static T ToObject<T>(this JsonDocument document)
    {
        var json = document.RootElement.GetRawText();
        return JsonSerializer.Deserialize<T>(json, _jsonOpts);
    }

    public static byte[] ToBytes(this JsonElement e)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(e, _jsonOpts);
        return bytes;
    }

    // Useful when we need to mutate an object (such as adding back properties)
    // but don't want to mutate the original.
    public static T JsonClone<T>(this T obj)
    {
        var str = JsonSerializer.Serialize(obj, _jsonOpts);
        var obj2 = JsonSerializer.Deserialize<T>(str, _jsonOpts);
        return obj2;
    }

}
