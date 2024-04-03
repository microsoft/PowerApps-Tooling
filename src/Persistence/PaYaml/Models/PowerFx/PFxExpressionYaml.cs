// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

// REVIEW: Consider implementing IYamlConvertible instead of using a custom converter.
public record PFxExpressionYaml(
    [property: YamlIgnore]
    string InvariantScript)
{
}
