// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using YamlDotNet.Core;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class FirstClassControlsEmitter : ChainedEventEmitter
{
    private static readonly ConcurrentDictionary<Type, string> _typeToNodeName = new();
    private readonly IControlTemplateStore _controlTemplateStore;

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
            var keySource = new ObjectDescriptor(nodeName, typeof(string), typeof(string));
            nextEmitter.Emit(new ScalarEventInfo(keySource), emitter);

            var valueSource = new ObjectDescriptor(null, typeof(string), typeof(string));
            nextEmitter.Emit(new ScalarEventInfo(valueSource), emitter);
        }
    }

    private bool CheckIsFirstClass(EventInfo eventInfo, out string nodeName)
    {
        nodeName = _typeToNodeName.GetOrAdd(eventInfo.Source.Type, type =>
        {
            if (type == typeof(BuiltInControl))
            {
                if (_controlTemplateStore.TryGetByUri(((Control)eventInfo.Source.Value!).ControlUri, out var controlTemplate))
                    return controlTemplate.Name;
            }
            if (!type.IsFirstClass(out var attrib))
                return string.Empty;

            return !string.IsNullOrWhiteSpace(attrib?.ShortName) ? attrib.ShortName : type.Name;
        });
        return !string.IsNullOrWhiteSpace(nodeName);
    }
}
