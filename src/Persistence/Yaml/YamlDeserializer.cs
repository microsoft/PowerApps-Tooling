// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlDeserializer : IYamlDeserializer
{
    private readonly IDeserializer _deserializer;

    internal YamlDeserializer(IDeserializer deserializer)
    {
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public T? Deserialize<T>(string yaml) where T : notnull
    {
        _ = yaml ?? throw new ArgumentNullException(nameof(yaml));

        using var reader = new StringReader(yaml);
        return DeserializeCore<T>(reader);
    }

    public T? Deserialize<T>(TextReader reader) where T : notnull
    {
        _ = reader ?? throw new ArgumentNullException(nameof(reader));

        return DeserializeCore<T>(reader);
    }

    private T? DeserializeCore<T>(TextReader reader) where T : notnull
    {
        try
        {
            return _deserializer.Deserialize<T>(reader);
        }
        catch (YamlException ex)
        {
            throw PaDiagnosticsException.FromYamlException(ex, PersistenceErrorCode.DeserializationError, originToolOrFilePath: null);
        }
    }
}
