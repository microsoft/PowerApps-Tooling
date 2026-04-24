// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Persistence.Tests;

public abstract class TestBase : VSTestBase
{
    protected TestBase()
    {
    }

    /// <summary>
    /// Indicates whether the current target framework is .NET Framework.
    /// </summary>
    /// <remarks>This constant is defined only when compiling for .NET Framework. Use it to conditionally
    /// execute code specific to this framework version.</remarks>
#if NETFRAMEWORK
    public static bool CurrentTfmIsNetFramework => true;
#else
    public static bool CurrentTfmIsNetFramework => false;
#endif

    /// <summary>
    /// Asserts that two JSON strings are structurally equivalent by comparing their <see cref="JsonNode"/> representations.
    /// </summary>
    protected static void JsonShouldBeEquivalentTo(string actualJson, string expectedJson)
    {
        // While we're detecting equality correct here, the failure message isn't particularly useful, but can be improved in the future.
        JsonNode.DeepEquals(JsonNode.Parse(actualJson), JsonNode.Parse(expectedJson))
            .Should().BeTrue($"actual JSON should be node-equivalent to expected JSON.\nActual:\n{actualJson}\nExpected:\n{expectedJson}");
    }

    /// <summary>
    /// Utility to create a <see cref="JsonElement"/> from a JSON string, which can be useful for constructing test inputs for models that use <see cref="JsonElement"/> properties.
    /// </summary>
    public static JsonElement ToJsonElement([StringSyntax(StringSyntaxAttribute.Json)] string json)
    {
        using var doc = JsonDocument.Parse(json);
        // We need to Clone so the element outlives 'doc' being disposed
        return doc.RootElement.Clone();
    }
}
