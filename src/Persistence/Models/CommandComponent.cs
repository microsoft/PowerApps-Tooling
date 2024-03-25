// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(templateName: BuiltInTemplates.CommandComponent)]
[YamlSerializable]
public record CommandComponent : Component
{
    [SetsRequiredMembers]
    public CommandComponent(string name, string variant, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Variant = variant;

        var baseTemplate = controlTemplateStore.GetByName(BuiltInTemplates.CommandComponent);
        Template = baseTemplate with { Name = name };
    }
}
