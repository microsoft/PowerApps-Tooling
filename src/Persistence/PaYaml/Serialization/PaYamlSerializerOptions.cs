// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public record PaYamlSerializerOptions
{
    public static readonly PaYamlSerializerOptions Default = new();

    public string NewLine { get; init; } = "\n";

    /// <summary>
    /// Maximum recursion depth allowed during deserialization.
    /// Bounds pathological/untrusted YAML input.
    /// </summary>
    public int? MaximumRecursion { get; init; }

    public PFxExpressionYamlFormattingOptions PFxExpressionYamlFormatting { get; init; } = new();

    public Action<DeserializerBuilder>? AdditionalDeserializerConfiguration { get; init; }

    public Action<SerializerBuilder>? AdditionalSerializerConfiguration { get; init; }
}
