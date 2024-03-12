// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class FirstClassControlsEmitter : ChainedEventEmitter
{
    private readonly IControlTemplateStore _controlTemplateStore;

    public FirstClassControlsEmitter(IEventEmitter nextEmitter, IControlTemplateStore controlTemplateStore)
       : base(nextEmitter)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
    {
        nextEmitter.Emit(eventInfo, emitter);

        if (CheckIsFirstClass(eventInfo, out var controlValue))
        {
            //// Handle printing out the name as a left-side value
            //var controlName = (eventInfo.Source.Value as Control)!.Name;
            //var controlNameDescriptor = new ObjectDescriptor(controlName, typeof(string), typeof(string));
            //var nullValueDescriptor = new ObjectDescriptor(null, typeof(string), typeof(string));
            //nextEmitter.Emit(new ScalarEventInfo(controlNameDescriptor), emitter);
            //nextEmitter.Emit(new ScalarEventInfo(nullValueDescriptor), emitter);

            //// start a new mapping layer for the rest of the control
            //nextEmitter.Emit(new MappingStartEventInfo(eventInfo.Source), emitter);

            // Print out control type
            var keySource = new ObjectDescriptor(YamlFields.Control, typeof(string), typeof(string));
            nextEmitter.Emit(new ScalarEventInfo(keySource), emitter);

            var valueSource = new ObjectDescriptor(controlValue, typeof(string), typeof(string));
            nextEmitter.Emit(new ScalarEventInfo(valueSource), emitter);
        }
    }

    public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
    {
        if (CheckIsFirstClass(eventInfo, out var _))
        {
            // close our extra layer of nesting for the control's properties
            //nextEmitter.Emit(new MappingEndEventInfo(eventInfo.Source), emitter);
        }

        base.Emit(eventInfo, emitter);
    }

    private bool CheckIsFirstClass(EventInfo eventInfo, [MaybeNullWhen(false)] out string nodeName)
    {
        if (_controlTemplateStore.TryGetById(((Control)eventInfo.Source.Value!).TemplateId, out var controlTemplate))
        {
            nodeName = controlTemplate.DisplayName;
            return true;
        }

        nodeName = null;
        return false;
    }
}
