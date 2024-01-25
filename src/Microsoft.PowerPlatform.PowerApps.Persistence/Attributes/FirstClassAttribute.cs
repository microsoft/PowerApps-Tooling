// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class FirstClassAttribute : Attribute
{
    public FirstClassAttribute(string? nodeName = null)
    {
        NodeName = nodeName ?? string.Empty;
    }
    public string NodeName { get; }
}
