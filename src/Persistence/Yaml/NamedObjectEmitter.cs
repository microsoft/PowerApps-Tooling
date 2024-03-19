// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class NamedObjectEmitter<T> : ChainedEventEmitter
    where T : class
{
    private readonly Func<T, string?> _nodeNameProvider;

    public NamedObjectEmitter(IEventEmitter nextEmitter, Func<T, string?> nodeNameProvider) : base(nextEmitter)
    {
        _nodeNameProvider = nodeNameProvider ?? throw new ArgumentNullException(nameof(nodeNameProvider));
    }

    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
    {
        nextEmitter.Emit(eventInfo, emitter);

        var value = eventInfo.Source.Value as T;
        if (value is null)
            return;

        var nodeName = _nodeNameProvider(value);
        if (string.IsNullOrWhiteSpace(nodeName))
            return;

        var keySource = new ObjectDescriptor(nodeName, typeof(string), typeof(string));
        nextEmitter.Emit(new ScalarEventInfo(keySource), emitter);

        var valueSource = new ObjectDescriptor(null, typeof(string), typeof(string));
        nextEmitter.Emit(new ScalarEventInfo(valueSource), emitter);
    }
}
