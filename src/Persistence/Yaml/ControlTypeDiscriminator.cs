// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ControlTypeDiscriminator : ITypeDiscriminator
{
    private readonly IControlTemplateStore _controlTemplateStore;

    public ControlTypeDiscriminator(IControlTemplateStore controlTemplateStore)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    public Type BaseType => typeof(object);

    public bool TryDiscriminate(IParser buffer, out Type? suggestedType)
    {
        suggestedType = null;

        if (!buffer.TryFindMappingEntry(TryFindTemplate, out var scalar, out var value))
        {
            suggestedType = typeof(CustomControl);
            return true;
        }

        // Control is abstract, so we need to return a concrete type.
        if (scalar!.Value == YamlFields.Control)
        {
            var templateId = ((Scalar)value!).Value.Trim();
            if (_controlTemplateStore.TryGetById(templateId, out var controlTemplate))
            {
                suggestedType = typeof(BuiltInControl);
                return true;
            }

            // If we don't have a type for this template, we'll use the custom control type.
            suggestedType = typeof(CustomControl);

            return true;
        }

        suggestedType = _controlTemplateStore.GetControlType(scalar!.Value);

        return true;
    }

    private bool TryFindTemplate(Scalar scalar)
    {
        if (scalar.Value == YamlFields.Control)
            return true;

        if (_controlTemplateStore.Contains(scalar.Value))
            return true;

        return false;
    }
}
