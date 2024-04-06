// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public class PaSerializationContext : IDisposable
{
    private readonly SerializerState _serializerState = new();
    private bool _isDisposed;

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    public IValueSerializer? ValueSerializer { get; set; }

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    public IValueDeserializer? ValueDeserializer { get; set; }

    public ObjectDeserializer CreateObjectDeserializer(IParser parser)
    {
        var valueDeserializer = ValueDeserializer ?? throw new InvalidOperationException($"{nameof(ValueDeserializer)} is not set.");

        return (t) => valueDeserializer.DeserializeValue(parser, t, _serializerState, valueDeserializer);
    }

    public ObjectSerializer CreateObjectSerializer(IEmitter emitter)
    {
        var valueSerializer = ValueSerializer ?? throw new InvalidOperationException($"{nameof(ValueSerializer)} is not set.");

        return (v, t) => valueSerializer.SerializeValue(emitter, v, t);
    }

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    public void OnDeserialization()
    {
        _serializerState.OnDeserialization();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _serializerState.Dispose();
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
