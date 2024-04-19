// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlSerializer : IYamlSerializer
{
    private readonly ISerializer _serializer;

    internal YamlSerializer(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public string Serialize(object graph)
    {
        _ = graph ?? throw new ArgumentNullException(nameof(graph));

        return SerializeCore(graph);
    }

    public string SerializeControl<T>(T graph) where T : Control
    {
        _ = graph ?? throw new ArgumentNullException(nameof(graph));

        return SerializeCore(graph);
    }

    public void SerializeControl<T>(TextWriter writer, T graph) where T : Control
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        _ = graph ?? throw new ArgumentNullException(nameof(graph));

        SerializeCore(writer, graph);
    }

    private void SerializeCore<T>(TextWriter writer, T graph)
    {
        try
        {
            _serializer.Serialize(writer, graph);
        }
        catch (YamlException ex)
        {
            throw ex.Start.Equals(Mark.Empty)
                ? new PersistenceException(PersistenceErrorCode.SerializationError, ex.Message, ex)
                : new PersistenceException(PersistenceErrorCode.SerializationError, ex.Message, ex)
                {
                    LineNumber = ex.Start.Line,
                    Column = ex.Start.Column,
                };
        }
    }

    private string SerializeCore<T>(T graph)
    {
        try
        {
            return _serializer.Serialize(graph);
        }
        catch (YamlException ex)
        {
            throw ex.Start.Equals(Mark.Empty)
                ? new PersistenceException(PersistenceErrorCode.SerializationError, ex.Message, ex)
                : new PersistenceException(PersistenceErrorCode.SerializationError, ex.Message, ex)
                {
                    LineNumber = ex.Start.Line,
                    Column = ex.Start.Column,
                };
        }
    }
}
