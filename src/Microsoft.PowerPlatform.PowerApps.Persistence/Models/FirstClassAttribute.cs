// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
internal class FirstClassAttribute : Attribute
{
    public FirstClassAttribute(string? nodeName = null)
    {
        NodeName = nodeName ?? string.Empty;
    }
    public string NodeName { get; }
}
