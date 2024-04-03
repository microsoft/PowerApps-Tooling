// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public record PFxExpressionYamlFormattingOptions
{
    public const char ScalarPrefix = '=';

    // Note: By default, we no longer force literal block when it contains a double quote, as these are not a problem when the '=' prefix is used.
    public IReadOnlyList<string>? ForceLiteralBlockIfContainsAny { get; init; } //= new[] { "\"" };
}
