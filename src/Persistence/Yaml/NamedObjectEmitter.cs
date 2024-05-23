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
internal sealed class NamedObjectEmitter(IEventEmitter nextEmitter) : ChainedEventEmitter(nextEmitter)
{
    private readonly Stack<bool> _isNamedObject = new();
    private bool _isName;

    public required YamlSerializationOptions Options { get; set; }

    /// <summary>
    /// Skip name emission if the object is a named object.
    /// </summary>
    /// <param name="eventInfo"></param>
    /// <param name="emitter"></param>
    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
    {
        nextEmitter.Emit(eventInfo, emitter);

        var namedObject = eventInfo.Source.Value as INamedObject;
        _isNamedObject.Push(namedObject != null);
    }

    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if (_isNamedObject.Peek())
        {
            if (eventInfo.Source.Value != null && eventInfo.Source.Value.Equals(nameof(INamedObject.Name)))
            {
                _isName = true;
                return;
            }

            // Skip name emission if the object is a named object.
            if (_isName)
            {
                _isName = false;
                return;
            }
        }
        nextEmitter.Emit(eventInfo, emitter);
    }

    public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
    {
        _isNamedObject.Pop();

        nextEmitter.Emit(eventInfo, emitter);
    }
}
