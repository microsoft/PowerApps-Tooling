// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public record YamlDeserializerOptions
{
    public bool IsTextFirst { get; init; } = true;

    public static readonly YamlDeserializerOptions Default = new()
    {
        IsTextFirst = true
    };
}
