// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record EditorStateInstance
{
    public string[]? ScreensOrder { get; init; }

    public string[]? ComponentDefinitionsOrder { get; init; }
}
