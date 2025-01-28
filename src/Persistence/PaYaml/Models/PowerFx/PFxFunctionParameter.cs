// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

public record PFxFunctionParameter()
{
    public string? Description { get; init; }

    public bool IsRequired { get; init; }

    public PFxDataType? DataType { get; init; }

    /// <summary>
    /// The default script for this optional parameter.
    /// </summary>
    public PFxExpressionYaml? Default { get; init; }
}
