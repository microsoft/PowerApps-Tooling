// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Represents a named object of type <typeparamref name="TValue"/>.<br/>
/// In YAML, as a value on its own, an instance is represented as a Mapping with a single entry, where the <paramref name="Name"/> is the key.<br/>
/// </summary>
public record NamedObject<TValue>(string Name, TValue Value) : NamedObject<string, TValue>(Name, Value)
    where TValue : notnull
{
}

/// <summary>
/// Represents a named object of type <typeparamref name="TValue"/>.<br/>
/// In YAML, as a value on its own, an instance is represented as a Mapping with a single entry, where the <paramref name="Name"/> is the key.<br/>
/// </summary>
public record NamedObject<TName, TValue>(TName Name, TValue Value) : INamedObject<TName, TValue>
    where TName : notnull
    where TValue : notnull
{
    [YamlIgnore]
    public PaYamlLocation? Start { get; init; }
}
