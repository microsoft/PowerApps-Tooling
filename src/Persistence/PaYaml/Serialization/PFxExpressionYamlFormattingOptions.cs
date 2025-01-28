// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public record PFxExpressionYamlFormattingOptions
{
    public const char ScalarPrefix = '=';

    // Note: By default, we no longer force literal block when it contains a double quote, as these are not a problem when the '=' prefix is used.
    public ImmutableArray<string> ForceLiteralBlockIfContainsAny { get; init; } = [];
}
