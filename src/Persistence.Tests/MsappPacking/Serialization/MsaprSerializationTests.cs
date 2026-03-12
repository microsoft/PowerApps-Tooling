// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Serialization;

namespace Persistence.Tests.MsappPacking.Serialization;

[TestClass]
public class MsaprSerializationTests : TestBase
{
    /// <summary>
    /// Verifies that a minimal MsaprHeaderJson survives a serialize/deserialize round-trip.
    /// </summary>
    [TestMethod]
    public void MsaprHeaderJson_RoundTrip_Minimal()
    {
        var original = new MsaprHeaderJson
        {
            MsaprStructureVersion = MsaprHeaderJson.CurrentMsaprStructureVersion,
            UnpackedConfiguration = new()
            {
                ContentTypes = [],
            },
        };

        var json = JsonSerializer.Serialize(original, MsaprSerialization.DefaultJsonSerializeOptions);
        var deserialized = JsonSerializer.Deserialize<MsaprHeaderJson>(json, MsaprSerialization.DefaultJsonSerializeOptions);

        deserialized.Should().NotBeNull();
        deserialized!.MsaprStructureVersion.Should().Be(MsaprHeaderJson.CurrentMsaprStructureVersion);
        deserialized.UnpackedConfiguration.Should().NotBeNull();
        deserialized.UnpackedConfiguration.ContentTypes.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a fully-populated MsaprHeaderJson (all fields set) survives a serialize/deserialize round-trip.
    /// </summary>
    [TestMethod]
    public void MsaprHeaderJson_RoundTrip_FullyPopulated()
    {
        var original = new MsaprHeaderJson
        {
            MsaprStructureVersion = MsaprHeaderJson.CurrentMsaprStructureVersion,
            UnpackedConfiguration = new()
            {
                ContentTypes = ["Yaml", "Assets"],
            },
        };

        var json = JsonSerializer.Serialize(original, MsaprSerialization.DefaultJsonSerializeOptions);
        var deserialized = JsonSerializer.Deserialize<MsaprHeaderJson>(json, MsaprSerialization.DefaultJsonSerializeOptions);

        deserialized.Should().NotBeNull();
        deserialized!.MsaprStructureVersion.Should().Be(MsaprHeaderJson.CurrentMsaprStructureVersion);
        deserialized.UnpackedConfiguration.ContentTypes.Should().BeEquivalentTo(["Yaml", "Assets"]);
    }

    /// <summary>
    /// Verifies that unknown/additional properties in the JSON are ignored (forward-compatible deserialization).
    /// </summary>
    [TestMethod]
    public void MsaprHeaderJson_RoundTrip_IgnoresUnknownProperties()
    {
        var jsonWithExtraFields = """
            {
                "MsaprStructureVersion": "0.1",
                "UnpackedConfiguration": {
                    "ContentTypes": ["Yaml"],
                    "FutureProperty": "some value"
                },
                "AnotherFutureTopLevelProperty": 42
            }
            """;

        var deserialized = JsonSerializer.Deserialize<MsaprHeaderJson>(jsonWithExtraFields, MsaprSerialization.DefaultJsonSerializeOptions);

        deserialized.Should().NotBeNull();
        deserialized!.MsaprStructureVersion.Should().Be(new Version(0, 1));
        deserialized.UnpackedConfiguration.ContentTypes.Should().BeEquivalentTo(["Yaml"]);

        // And we should see the unknown properties still captured:
        deserialized.AdditionalProperties.Should().NotBeNull()
            .And.Subject.Keys.Should().BeEquivalentTo(["AnotherFutureTopLevelProperty"]);
        deserialized.UnpackedConfiguration.AdditionalProperties.Should().NotBeNull()
            .And.Subject.Keys.Should().BeEquivalentTo(["FutureProperty"]);

        // Re-serializing should produce JSON node-equivalent to the original input.
        var reserialized = JsonSerializer.Serialize(deserialized, MsaprSerialization.DefaultJsonSerializeOptions);
        JsonShouldBeEquivalentTo(reserialized, jsonWithExtraFields);
    }
}
