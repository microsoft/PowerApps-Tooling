// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public record YamlDeserializerOptions
{
    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Explicitly setting to false for clarity")]
    public bool IsTextFirst { get; init; } = false;

    public static readonly YamlDeserializerOptions Default = new()
    {
        IsTextFirst = false
    };
}
