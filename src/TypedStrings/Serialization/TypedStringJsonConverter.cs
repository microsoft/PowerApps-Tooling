// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.TypedStrings;

public class TypedStringJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        // We can only convert non-generic, concrete classes
        if (typeToConvert.IsGenericType || !typeToConvert.IsClass || typeToConvert.IsAbstract)
        {
            return false;
        }

        var expectedTypeDef = typeof(ITypedString<>);
        var expectedType = expectedTypeDef.MakeGenericType(typeToConvert);
        return typeToConvert.IsAssignableTo(expectedType);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter?)Activator.CreateInstance(
            typeof(StrongTypedStringJsonConverterInner<>).MakeGenericType([typeToConvert]),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null);
    }

    private sealed class StrongTypedStringJsonConverterInner<T> : JsonConverter<T>
        where T : class, ITypedString<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.String);
            var str = reader.GetString()!;
            // Todo - replace validation
            //Contracts.AssertValue(str, "GetString should've returned non-null when the TokenType is String.");

            if (!T.TryParse(str, out var result))
            {
                throw new JsonException($"Invalid value for {typeof(T).Name}: {str}");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            var str = reader.GetString();

            if (!T.TryParse(str, out var result))
            {
                throw new JsonException($"Invalid value for {typeof(T).Name} as a json property name: {str}");
            }

            return result;
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.Value);
        }
    }
}
