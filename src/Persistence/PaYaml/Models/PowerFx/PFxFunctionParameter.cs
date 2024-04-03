// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

public record PFxFunctionParameter()
{
    public string? Description { get; init; }

    public bool IsRequired { get; init; }

    public PFxDataType? DataType { get; init; }

    public PFxExpressionYaml? Default { get; init; }
}
