// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

/// <summary>
/// Optional parameters for creating an instance of a control or screen.
/// This object only contains editor-state parameters which are not generally known to customers/authors, and is not expected to be modified by them.
/// </summary>
public record InstanceCreationParameters : ISupportsIsEmpty
{
    public string? Variant { get; init; }

    public string? Layout { get; init; }

    public ParentTemplateCreationParameter? ParentTemplate { get; init; }

    public string? MetadataId { get; init; }

    public string? StyleName { get; init; }

    public bool IsEmpty()
    {
        return Variant == null
            && Layout == null
            && (ParentTemplate == null || ParentTemplate.IsEmpty())
            && MetadataId == null
            && StyleName == null;
    }
}

public class ParentTemplateCreationParameter : ISupportsIsEmpty
{
    public string? CompositionName { get; init; }

    public string? Variant { get; init; }

    public bool IsEmpty()
    {
        return CompositionName == null && Variant == null;
    }
}
