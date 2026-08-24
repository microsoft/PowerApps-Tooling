// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

/// <summary>
/// A node deserializer that wraps an existing <see cref="INodeDeserializer"/> which will set the location information on the value if it implements <see cref="IMayHavePaYamlLocation"/>.
/// </summary>
internal sealed class SetNodeLocationNodeDeserializerWrapper<TNodeDeserializerToWrap>(TNodeDeserializerToWrap wrappedNodeDeserializer) : INodeDeserializer
    where TNodeDeserializerToWrap : INodeDeserializer
{
    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
    {
        // Capture the start location before deserializing the node
        var start = reader.Current?.Start;
        if (wrappedNodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer))
        {
            if (start != null && value is ISetPaYamlNodeLocation setNodeLocationValue)
            {
                // Only set the location if it hasn't been set yet
                if (setNodeLocationValue.Start is null)
                {
                    setNodeLocationValue.SetNodeLocation(PaYamlLocation.FromMark(start.Value));
                }
            }

            return true;
        }

        return false;
    }
}

internal static class SetNodeLocationNodeDeserializerWrapperExtensions
{
    /// <summary>
    /// Wraps the specified <typeparamref name="TNodeDeserializerToWrap"/> with a <see cref="SetNodeLocationNodeDeserializerWrapper{TNodeDeserializerToWrap}"/> which will set the location information on the value if it implements <see cref="IMayHavePaYamlLocation"/>.
    /// </summary>
    /// <typeparam name="TNodeDeserializerToWrap">The type of the <see cref="INodeDeserializer"/> to be replaced and wrapped.</typeparam>
    public static DeserializerBuilder WithSetNodeLocationNodeDeserializerWrapper<TNodeDeserializerToWrap>(this DeserializerBuilder builder)
        where TNodeDeserializerToWrap : INodeDeserializer
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        return builder.WithNodeDeserializer(
            baseDeserializer => new SetNodeLocationNodeDeserializerWrapper<TNodeDeserializerToWrap>((TNodeDeserializerToWrap)baseDeserializer),
            s => s.InsteadOf<TNodeDeserializerToWrap>());
    }
}
