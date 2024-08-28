// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class YamlExtensions
{
    internal static readonly char[] LineTerminators = new char[] { '\r', '\n', '\x85', '\x2028', '\x2029' };

    public static ScalarStyle DetermineScalarStyleForProperty(this string? property)
    {
        if (property == null)
        {
            return ScalarStyle.Plain;
        }
        else if (property.Any(c => LineTerminators.Contains(c)))
        {
            return ScalarStyle.Literal;
        }
        else if (property.Contains(" #") || property.Contains(": "))
        {
            // These sequences break YAML parsing when outside of a literal block
            return ScalarStyle.Literal;
        }
        else
        {
            return ScalarStyle.Plain;
        }
    }

    public static void Emit(this IEmitter emitter, string propertyName, string? propertyValue)
    {
        if (string.IsNullOrWhiteSpace(propertyValue))
            return;

        emitter.Emit(new Scalar(propertyName));
        emitter.Emit(new Scalar(null, null, propertyValue, propertyValue.DetermineScalarStyleForProperty(), true, false));
    }
}
