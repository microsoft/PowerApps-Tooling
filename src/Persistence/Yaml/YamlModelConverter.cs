// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
internal class YamlModelConverter : IYamlTypeConverter
{
    public IValueSerializer? ValueSerializer { get; set; }
    public IValueDeserializer? ValueDeserializer { get; set; }


    public bool Accepts(Type type)
    {
        return type == typeof(Model.Control);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        _ = ValueDeserializer ?? throw new ArgumentNullException(nameof(ValueDeserializer));
        parser.Consume<MappingStart>();

        var nameScalar = parser.Consume<Scalar>();
        using var serializerState = new YamlDotNet.Serialization.Utilities.SerializerState();
        var value = ValueDeserializer.DeserializeValue(parser, typeof(Model.ControlInstance), serializerState, ValueDeserializer);
        parser.Consume<MappingEnd>();

        return new Model.Control { Name = nameScalar.Value, Data = (value as Model.ControlInstance)! };
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        _ = ValueSerializer ?? throw new ArgumentNullException(nameof(ValueSerializer));

        var val = value as Model.Control;
        emitter.Emit(new MappingStart());
        emitter.Emit(new Scalar(val!.Name));

        ValueSerializer.SerializeValue(emitter, val.Data, typeof(Model.ControlInstance));

        emitter.Emit(new MappingEnd());
    }
}
