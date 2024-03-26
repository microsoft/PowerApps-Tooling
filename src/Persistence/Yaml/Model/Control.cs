// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml.Model;

/// <summary>
/// The de-/serialization of this class should be handled by <see cref="YamlModelConverter"/>,
/// so YamlDotNet attributes on things here will be ignored.
/// </summary>
public record Control
{
    [YamlIgnore] // the Name Property's value will be set as the Data property's key via TpeConverter
    public required string Name { get; init; }

    public required ControlInstance Data { get; init; }
}

public record ControlInstance
{
    [YamlMember(Alias = "Control")]
    public required string ControlType { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    public IReadOnlyList<Control>? Children { get; init; }
}
