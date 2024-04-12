// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record ComponentInstance : Control
{
    /// <summary>
    /// The template id for a local component instance.
    /// </summary>
    public const string ComponentInstanceTemplateId = "http://localhost/Component";

    public ComponentInstance()
    {
    }

    [SetsRequiredMembers]
    public ComponentInstance(string name, string variant, ControlTemplate controlTemplate) : base(name, variant, controlTemplate)
    {
    }

    [YamlMember(Order = 1)]
    public string ComponentName { get; set; } = string.Empty;
}
