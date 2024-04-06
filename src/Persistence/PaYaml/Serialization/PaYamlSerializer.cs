// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public static class PaYamlSerializer
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    #region Serialize

    /// <summary>
    /// Converts the value of a type specified by a generic type parameter into a YAML string.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">Options to control serialization behavior.</param>
    /// <returns></returns>
    public static string Serialize<TValue>(TValue value, PaYamlSerializerOptions? options = null)
    {
        var sb = new StringBuilder();
        using (var writer = new StringWriter(sb, CultureInfo.InvariantCulture))
        {
            WriteTextWriter(writer, value, options);
        }
        return sb.ToString();
    }

    public static void Serialize<TValue>(Stream utf8Stream, TValue value, PaYamlSerializerOptions? options = null)
    {
        _ = utf8Stream ?? throw new ArgumentNullException(nameof(utf8Stream));

        using (var writer = new StreamWriter(utf8Stream, Utf8NoBom))
        {
            WriteTextWriter(writer, value, options);
        }
    }

    private static void WriteTextWriter<TValue>(TextWriter writer, in TValue? value, PaYamlSerializerOptions? options)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));

        options ??= PaYamlSerializerOptions.Default;
        var targetType = typeof(TValue);

        // Configure the YamlDotNet serializer
        var serializationContext = new SerializationContext();
        var builder = new SerializerBuilder();
        options.ApplyToSerializerBuilder(builder, serializationContext);
        serializationContext.ValueSerializer = builder.BuildValueSerializer();
        var serializer = builder.Build();

        serializer.Serialize(writer, value, targetType);
    }
    #endregion

    #region Deserialize

    /// <summary>
    /// Parses the text representing a single YAML value into an instance of the type specified by a generic type parameter.
    /// </summary>
    /// <typeparam name="TValue">The target type of the YAML value.</typeparam>
    /// <param name="yaml">The YAML text to parse.</param>
    /// <param name="options">Options to control the behavior during parsing.</param>
    /// <returns></returns>
    public static TValue? Deserialize<TValue>(string yaml, PaYamlSerializerOptions? options = null)
        where TValue : notnull
    {
        _ = yaml ?? throw new ArgumentNullException(nameof(yaml));

        using (var reader = new StringReader(yaml))
        {
            return ReadFromReader<TValue>(reader, options);
        }
    }

    public static TValue? Deserialize<TValue>(Stream utf8Stream, PaYamlSerializerOptions? options = null)
        where TValue : notnull
    {
        _ = utf8Stream ?? throw new ArgumentNullException(nameof(utf8Stream));

        using (var reader = new StreamReader(utf8Stream, detectEncodingFromByteOrderMarks: true))
        {
            return ReadFromReader<TValue>(reader, options);
        }
    }

    private static TValue? ReadFromReader<TValue>(TextReader reader, PaYamlSerializerOptions? options)
        where TValue : notnull
    {
        _ = reader ?? throw new ArgumentNullException(nameof(reader));

        options ??= PaYamlSerializerOptions.Default;

        // Configure the YamlDotNet serializer
        var serializationContext = new SerializationContext();
        var builder = new DeserializerBuilder();
        options.ApplyToDeserializerBuilder(builder, serializationContext);
        serializationContext.ValueDeserializer = builder.BuildValueDeserializer();
        var serializer = builder.Build();

        return serializer.Deserialize<TValue>(reader);
    }
    #endregion
}
