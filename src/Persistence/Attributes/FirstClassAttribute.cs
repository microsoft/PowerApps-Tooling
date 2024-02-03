// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class FirstClassAttribute : Attribute
{
    public FirstClassAttribute(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException($"'{nameof(templateName)}' cannot be null or whitespace.", nameof(templateName));

        TemplateName = templateName;
    }

    public string TemplateName { get; }
}
