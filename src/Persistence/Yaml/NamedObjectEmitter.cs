// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

/// <summary>
/// YAML emitter that uses a predicate to determine the node name for a given object.
/// Would result in a YAML node with the resolved name of the object, and the object's value. For example:
///    MyName:
///    Property1: Value1
///    Property2: Value2
/// </summary>
/// <typeparam name="T"></typeparam>
internal class NamedObjectEmitter<T> : ChainedEventEmitter
    where T : class
{
    private readonly Func<T, string?> _nodeNameProvider;

    public NamedObjectEmitter(IEventEmitter nextEmitter, Func<T, string?> nodeNameProvider) : base(nextEmitter)
    {
        _nodeNameProvider = nodeNameProvider ?? throw new ArgumentNullException(nameof(nodeNameProvider));
    }

    /// <summary>
    /// generates a scalar event for the object's name, and then a mapping start event for the object's value.
    /// if the object is not of type <typeparamref name="T"/> or the resolved name is null or empty, no custom events are emitted.
    /// </summary>
    /// <param name="eventInfo"></param>
    /// <param name="emitter"></param>
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
