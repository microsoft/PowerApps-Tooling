// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class FirstClassControlsEmitter : NamedObjectEmitter<Control>
{
    public FirstClassControlsEmitter(IEventEmitter nextEmitter, IControlTemplateStore controlTemplateStore)
       : base(nextEmitter, c => GetControlName(c, controlTemplateStore))
    {

    }

    private static string? GetControlName(Control control, IControlTemplateStore controlTemplateStore)
    {
        if (control.Template != null && control.Template.HasDisplayName)
        {
            return control.Template.DisplayName;
        }

        if (controlTemplateStore.TryGetById(control.TemplateId, out var controlTemplate))
        {
            return controlTemplate.DisplayName;
        }

        return null;
    }
}
