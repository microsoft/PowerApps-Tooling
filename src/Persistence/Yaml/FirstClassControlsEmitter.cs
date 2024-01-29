// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using YamlDotNet.Core;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class FirstClassControlsEmitter : ChainedEventEmitter
{
    private static readonly ConcurrentDictionary<Type, string> _typeToNodeName = new();

    public FirstClassControlsEmitter(IEventEmitter nextEmitter)
       : base(nextEmitter) { }

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

    private static bool CheckIsFirstClass(EventInfo eventInfo, out string nodeName)
    {
        nodeName = _typeToNodeName.GetOrAdd(eventInfo.Source.Type, type =>
        {
            if (!type.IsFirstClass(out var attrib))
                return string.Empty;

            return !string.IsNullOrWhiteSpace(attrib?.ShortName) ? attrib.ShortName : type.Name;
        });
        return !string.IsNullOrWhiteSpace(nodeName);
    }
}
