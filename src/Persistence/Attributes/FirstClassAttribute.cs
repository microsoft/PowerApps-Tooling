// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class FirstClassAttribute : Attribute
{
    public FirstClassAttribute(string shortName, string templateUri)
    {
        if (string.IsNullOrWhiteSpace(shortName))
            throw new ArgumentException($"'{nameof(shortName)}' cannot be null or whitespace.", nameof(shortName));
        if (string.IsNullOrWhiteSpace(templateUri))
            throw new ArgumentException($"'{nameof(templateUri)}' cannot be null or whitespace.", nameof(templateUri));

        ShortName = shortName;
        TemplateUri = templateUri;
    }

    public string ShortName { get; }

    public string TemplateUri { get; }
}
