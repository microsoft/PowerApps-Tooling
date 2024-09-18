// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

/// <summary>
/// custom converter used to test if the input is a sequence of YAML items.
/// </summary>
internal sealed class YamlSequenceTesterConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(IEnumerable) || type.IsSubclassOf(typeof(IEnumerable)) ||
            type == typeof(IEnumerable<object>) || type.IsSubclassOf(typeof(IEnumerable<object>)) ||
            type == typeof(object[]);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        if (parser.Current is not SequenceStart)
            throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected sequence start but got {parser.Current.GetType().Name}");

        while (!parser.Accept<SequenceEnd>(out _))
        {
            parser.MoveNext();
        }

        parser.MoveNext();

        return Array.Empty<object>();
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        throw new NotImplementedException();
    }
}
