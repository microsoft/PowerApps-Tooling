// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Serialization;

namespace Persistence.Tests.PaYaml.Serialization;

[TestClass]
public class NamedObjectSequenceSerializationTests : SerializationTestBase
{
    protected override void ConfigureYamlDotNetDeserializer(DeserializerBuilder builder, PaYamlSerializationContext context)
    {
        base.ConfigureYamlDotNetDeserializer(builder, context);
        builder.WithTypeConverter(new NamedObjectYamlConverter<string>(context));
    }

    protected override void ConfigureYamlDotNetSerializer(SerializerBuilder builder, PaYamlSerializationContext context)
    {
        base.ConfigureYamlDotNetSerializer(builder, context);
        builder.WithTypeConverter(new NamedObjectYamlConverter<string>(context));
    }

    [TestMethod]
    // Null literals
    [DataRow("TheSequence: ~", null)]
    [DataRow("TheSequence: Null", null)]
    // Empty mappings:
    [DataRow("TheSequence: ", null)]
    [DataRow("TheSequence:", null)]
    [DataRow("TheSequence: []", 0)]
    // Flow style mappings:
    [DataRow("TheSequence: [{n1: v1}]", 1)]
    [DataRow("TheSequence: [{n1: v1}, {n2: v2}, {n3: v3}]", 3)]
    // Block style mappings:
    [DataRow("TheSequence: \n- n1: v1", 1)] // items not indented
    [DataRow("TheSequence: \n  - n1: v1\n  - n2: v2\n  - n3: v3", 3)] // each item is indented
    public void ReadYamlMappingOfStrings(string yaml, int? expectedCount)
    {
        VerifyDeserialize<string>(yaml, expectedCount);
    }

    private void VerifyDeserialize<TValue>(string yaml, int? expectedCount, string[]? expectedNames = null) where TValue : notnull
    {
        var testObject = DeserializeViaYamlDotNet<TestOM<string>>(yaml);
        testObject.ShouldNotBeNull();
        if (expectedCount is null)
        {
            testObject.TheSequence.Should().BeNullOrEmpty();
        }
        else
        {
            testObject.TheSequence.ShouldNotBeNull();
            testObject.TheSequence.Should().HaveCount(expectedCount.Value);

            if (expectedNames is not null)
            {
                testObject.TheSequence.Should().ContainNames(expectedNames);
            }
        }
    }

    [TestMethod]
    public void ReadYamlMappingSetsNamedObjectStart()
    {
        var yaml = @"TheSequence:
  - n1: v1
  # comment line
  - n3: |-
     v3
  -
   n2: v2
";
        var testObject = DeserializeViaYamlDotNet<TestOM<string>>(yaml);
        testObject.ShouldNotBeNull();
        testObject.TheSequence.ShouldNotBeNull();
        testObject.TheSequence.Names.Should().Equal(new[] { "n1", "n3", "n2" }, "ordering of a sequence is by code order");
        testObject.TheSequence.GetNamedObject("n1").Should()
            .HaveValueEqual("v1")
            .And.HaveStartEqual(2, 5);
        testObject.TheSequence.GetNamedObject("n2").Should()
            .HaveValueEqual("v2")
            .And.HaveStartEqual(7, 4);
        testObject.TheSequence.GetNamedObject("n3").Should()
            .HaveValueEqual("v3")
            .And.HaveStartEqual(4, 5);
    }

    [TestMethod]
    public void SerializeAsPropertyBeingNullOrEmpty()
    {
        SerializeViaYamlDotNet(new TestOM<string> { TheSequence = null })
            .Should().Be("{}" + DefaultOptions.NewLine);
        SerializeViaYamlDotNet(new TestOM<string> { TheSequence = new() })
            .Should().Be("{}" + DefaultOptions.NewLine);
    }

    public record TestOM<TValue>
        where TValue : notnull
    {
        public NamedObjectSequence<TValue>? TheSequence { get; init; }
    }
}
