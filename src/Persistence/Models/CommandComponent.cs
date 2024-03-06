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
    public CommandComponent(string name, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.CommandComponent);
    }
}
