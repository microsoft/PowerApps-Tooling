// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record EditorStateInstance
{
    public ImmutableArray<string>? ScreensOrder { get; init; }

    public ImmutableArray<string>? ComponentDefinitionsOrder { get; init; }
}
