// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.Schemas;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

internal static class PaYamlDotNetExtensions
{
    private static readonly NullNodeDeserializer _nullNodeDeserializer = new();
    private static readonly Type _typeofObject = typeof(object);
    private static readonly Scalar _nullImplicitPlainScalar = new(null, JsonSchema.Tags.Null, string.Empty, ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false);

    /// <summary>
    /// If the current event represents a YAML null value, consumes it and returns true; otherwise, returns false.
    /// </summary>
    /// <returns>true if the current event represented a YAML null value and the event was consumed.</returns>
    public static bool TryConsumeNull(this IParser parser)
    {
        // NullNodeDeserializer.Deserialize is undocumented, but here's a good summary of what it does:
        // Attempts to consume the current node event iif it represents a YAML null value. Otherwise, the current event stays.
        // Returns true if the current node was a null value and was consumed; otherwise, false.
        return _nullNodeDeserializer.Deserialize(parser, _typeofObject, null!, out _);
    }

    public static void EmitNull(this IEmitter emitter)
    {
        emitter.Emit(_nullImplicitPlainScalar);
    }

    public static PaYamlLocation? ToYamlLocation(this Mark mark)
    {
        if (mark.Equals(Mark.Empty))
        {
            return null;
        }

        return new PaYamlLocation(mark.Line, mark.Column);
    }
}
