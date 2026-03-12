// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;

namespace Persistence.Tests.MsApp.Serialization;

[TestClass]
public class MsappSerializationTests : TestBase
{
    private static readonly JsonSerializerOptions Options = MsappSerialization.PackedJsonSerializeOptions;

    /// <summary>
    /// Verifies that a PackedJson with LoadFromYaml=true survives a serialize/deserialize round-trip.
    /// </summary>
    [TestMethod]
    public void PackedJson_RoundTrip_LoadFromYaml_True()
    {
        var original = new PackedJson
        {
            PackedStructureVersion = PackedJson.CurrentPackedStructureVersion,
            LoadConfiguration = new() { LoadFromYaml = true },
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<PackedJson>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.LoadConfiguration.LoadFromYaml.Should().BeTrue();
        deserialized.PackedStructureVersion.Should().Be(PackedJson.CurrentPackedStructureVersion);
    }

    /// <summary>
    /// Verifies that a PackedJson with LoadFromYaml=false survives a serialize/deserialize round-trip.
    /// With WhenWritingDefault, the 'false' value is the default for bool and will be omitted during
    /// serialization. Because LoadFromYaml is marked 'required', deserialization then fails unless
    /// the serializer options are corrected.
    /// </summary>
    [TestMethod]
    public void PackedJson_RoundTrip_LoadFromYaml_False()
    {
        var original = new PackedJson
        {
            PackedStructureVersion = PackedJson.CurrentPackedStructureVersion,
            LoadConfiguration = new() { LoadFromYaml = false },
        };

        var json = JsonSerializer.Serialize(original, Options);

        // The serialized JSON must contain the LoadFromYaml property, even when false,
        // because it is a 'required' property on deserialization.
        json.Should().Contain("LoadFromYaml", "required bool properties must not be omitted when their value is the default (false)");

        var deserialized = JsonSerializer.Deserialize<PackedJson>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.LoadConfiguration.LoadFromYaml.Should().BeFalse();
        deserialized.PackedStructureVersion.Should().Be(PackedJson.CurrentPackedStructureVersion);
    }

    /// <summary>
    /// Round-trips a fully-populated PackedJson (all optional fields set) to ensure nothing is lost.
    /// </summary>
    [TestMethod]
    public void PackedJson_RoundTrip_FullyPopulated()
    {
        var utcNow = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var original = new PackedJson
        {
            PackedStructureVersion = PackedJson.CurrentPackedStructureVersion,
            LastPackedDateTimeUtc = utcNow,
            PackingClient = new PackedJsonPackingClient
            {
                Name = "TestClient",
                Version = "1.2.3",
            },
            LoadConfiguration = new() { LoadFromYaml = true },
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<PackedJson>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.PackedStructureVersion.Should().Be(PackedJson.CurrentPackedStructureVersion);
        deserialized.LastPackedDateTimeUtc.Should().Be(utcNow);
        deserialized.PackingClient.Should().NotBeNull();
        deserialized.PackingClient!.Name.Should().Be("TestClient");
        deserialized.PackingClient.Version.Should().Be("1.2.3");
        deserialized.LoadConfiguration.LoadFromYaml.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a PackedJson with LoadFromYaml=false and a PackingClient survives round-trip.
    /// </summary>
    [TestMethod]
    public void PackedJson_RoundTrip_LoadFromYaml_False_WithPackingClient()
    {
        var original = new PackedJson
        {
            PackedStructureVersion = PackedJson.CurrentPackedStructureVersion,
            PackingClient = new PackedJsonPackingClient { Name = "MyCli", Version = "0.0.1" },
            LoadConfiguration = new() { LoadFromYaml = false },
        };

        var json = JsonSerializer.Serialize(original, Options);
        json.Should().Contain("LoadFromYaml", "required bool must be present in JSON even when false");

        var deserialized = JsonSerializer.Deserialize<PackedJson>(json, Options);
        deserialized.Should().NotBeNull();
        deserialized!.LoadConfiguration.LoadFromYaml.Should().BeFalse();
        deserialized.PackingClient!.Name.Should().Be("MyCli");
    }
}
