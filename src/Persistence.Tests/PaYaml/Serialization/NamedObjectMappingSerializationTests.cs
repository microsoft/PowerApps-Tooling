// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Persistence.Tests.PaYaml.Serialization;

[TestClass]
public class NamedObjectMappingSerializationTests : SerializationTestBase
{
    [TestMethod]
    // Null literals
    [DataRow("TheMapping: ~", null)]
    [DataRow("TheMapping: Null", null)]
    // Empty mappings:
    [DataRow("TheMapping: ", null)]
    [DataRow("TheMapping:", null)]
    [DataRow("TheMapping: {}", 0)]
    // Flow style mappings:
    [DataRow("TheMapping: {n1: v1}", 1)]
    [DataRow("TheMapping: {n1: v1, n2: v2, n3: v3}", 3)]
    // Block style mappings:
    [DataRow("TheMapping: \n  n1: v1", 1)]
    [DataRow("TheMapping: \n  n1: v1\n  n2: v2\n  n3: v3", 3)]
    public void ReadYamlMappingOfStrings(string yaml, int? expectedCount)
    {
        VerifyDeserialize<string>(yaml, expectedCount);
    }

    private void VerifyDeserialize<TValue>(string yaml, int? expectedCount, string[]? expectedNames = null) where TValue : notnull
    {
        var testObject = DeserializeViaYamlDotNet<TestOM<TValue>>(yaml);
        testObject.ShouldNotBeNull();
        if (expectedCount is null)
        {
            testObject.TheMapping.Should().BeNullOrEmpty();
        }
        else
        {
            testObject.TheMapping.ShouldNotBeNull();
            testObject.TheMapping.Should().HaveCount(expectedCount.Value);

            if (expectedNames is not null)
            {
                testObject.TheMapping.Should().ContainNames(expectedNames);
            }
        }
    }

    private static readonly string[] expected = ["n1", "n2", "n3"];

    [TestMethod]
    public void ReadYamlMappingSetsNamedObjectStart()
    {
        var yaml = @"TheMapping:
  n1: v1
  # comment line
  n3: |-
    v3
  n2: v2
";
        var testObject = DeserializeViaYamlDotNet<TestOM<string>>(yaml);
        testObject.ShouldNotBeNull();
        testObject.TheMapping.ShouldNotBeNull();
        testObject.TheMapping.Names.Should().Equal(expected, "ordering of a mapping is by name");
        testObject.TheMapping.GetNamedObject("n1").Should()
            .HaveValueEqual("v1")
            .And.HaveStartEqual(2, 3);
        testObject.TheMapping.GetNamedObject("n2").Should()
            .HaveValueEqual("v2")
            .And.HaveStartEqual(6, 3);
        testObject.TheMapping.GetNamedObject("n3").Should()
            .HaveValueEqual("v3")
            .And.HaveStartEqual(4, 3);
    }

    [TestMethod]
    public void SerializeAsPropertyBeingNullOrEmpty()
    {
        SerializeViaYamlDotNet(new TestOM<string> { TheMapping = null })
            .Should().Be("{}" + DefaultOptions.NewLine);
        SerializeViaYamlDotNet(new TestOM<string> { TheMapping = new() })
            .Should().Be("{}" + DefaultOptions.NewLine);
    }

    public record TestOM<TValue>
        where TValue : notnull
    {
        public NamedObjectMapping<TValue>? TheMapping { get; init; }
    }
}
