// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Persistence.Tests.PaYaml.Models;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1861: Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array", Justification = "Obfuscates tests")]
public class NamedObjectSequenceTests : TestBase
{
    [TestMethod]
    public void NamedObjectSequenceCollectionInitializerTest()
    {
        var namedObjectMapping = new NamedObjectSequence<string>
        {
            // These will use the Add(NamedObject<TValue>) method overload:
            new NamedObject<string>("name1", "value1"),
            new("name2", "value2"),

            // This syntax uses the Add(string name, TValue value) method overload:
            { "name3", "value3" },
        };

        namedObjectMapping.Should().HaveCount(3);
        namedObjectMapping.Names.Should().BeEquivalentTo(["name1", "name2", "name3"]);
        namedObjectMapping["name1"].Should().Be("value1");
        namedObjectMapping["name2"].Should().Be("value2");
        namedObjectMapping["name3"].Should().Be("value3");
    }

    [TestMethod]
    public void NamedObjectSequenceOrderingTest()
    {
        var namedObjectMapping = new NamedObjectSequence<string>
        {
            new("name1", "value1"),
            new("name2", "value2"),
            new("name0", "value0"),
            new("namez", "valuez"),
            new("nameA", "valueA"),
            new("nameB", "valueB"),
            new("namea", "valuea"),
        };
        var expectedNamesInOrder = new[]
        {
            "name1",
            "name2",
            "name0",
            "namez",
            "nameA",
            "nameB",
            "namea",
        };

        namedObjectMapping.Names.Should().Equal(expectedNamesInOrder);
        namedObjectMapping.Select(no => no.Name).Should().Equal(expectedNamesInOrder, "Enumerator should emit items in the order they were added");
    }
}
