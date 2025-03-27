// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record EditorStateInstance
{
    public IList<string>? ScreensOrder { get; init; }

    public IList<string>? ComponentDefinitionsOrder { get; init; }
}
