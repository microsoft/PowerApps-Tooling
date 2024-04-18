// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal sealed class FirstClassControlsEmitter : ChainedEventEmitter
{
    private readonly IControlTemplateStore _controlTemplateStore;
    private readonly IReadOnlySet<string> _shortNameTypes = new HashSet<string> { "App", "Host", "Screen" };

    public FirstClassControlsEmitter(IEventEmitter nextEmitter, IControlTemplateStore controlTemplateStore)
       : base(nextEmitter)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
    {
        nextEmitter.Emit(eventInfo, emitter);

        if (CheckIsFirstClass(eventInfo, out var nodeName))
        {
            ObjectDescriptor keySource;
            ObjectDescriptor valueSource;
            if (_shortNameTypes.Contains(nodeName))
            {
                keySource = new ObjectDescriptor(nodeName, typeof(string), typeof(string));
                valueSource = new ObjectDescriptor(null, typeof(string), typeof(string));
            }
            else
            {
                keySource = new ObjectDescriptor(YamlFields.Control, typeof(string), typeof(string));
                valueSource = new ObjectDescriptor(nodeName, typeof(string), typeof(string));
            }
            nextEmitter.Emit(new ScalarEventInfo(keySource), emitter);
            nextEmitter.Emit(new ScalarEventInfo(valueSource), emitter);
        }
    }

    private bool CheckIsFirstClass(EventInfo eventInfo, [MaybeNullWhen(false)] out string nodeName)
    {
        var control = eventInfo.Source.Value as Control;
        if (control == null)
        {
            nodeName = null;
            return false;
        }

        // If the control has a template, use the template name
        if (control.Template != null && control.Template.HasDisplayName)
        {
            nodeName = control.Template.DisplayName;
            return true;
        }

        // If template is not found, look for the template by id
        if (_controlTemplateStore.TryGetById(control.TemplateId, out var controlTemplate))
        {
            nodeName = controlTemplate.DisplayName;
            return true;
        }

        nodeName = null;
        return false;
    }
}
