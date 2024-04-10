// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Yaml;

[TestClass]
public class ZIndexOrderingTests : TestBase
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SerializedChildrenInZOrder(bool isControlIdentifiers)
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new() { { "Text", "\"I am a screen\"" }, },
            children: new Control[]
            {
                MakeLabelWithZIndex(1),
                MakeLabelWithZIndex(3),
                MakeLabelWithZIndex(2),
                MakeLabelWithZIndex(5),
                MakeLabelWithZIndex(4),
            }
        );

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph);
        var expected = File.ReadAllText(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/Screen-with-sorted-children.pa.yaml", isControlIdentifiers));
        sut.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DeserializeChildrenShouldHaveMatchingZIndexProperty(bool isControlIdentifiers)
    {
        var deserializer = CreateDeserializer(isControlIdentifiers);

        using var yamlStream = File.OpenRead(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/Screen-with-sorted-children.pa.yaml", isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        var sut = deserializer.Deserialize<Screen>(yamlReader);

        sut.Children.Should()
            .HaveCount(5)
            .And.BeEquivalentTo(new List<Control>
            {
                MakeLabelWithZIndex(5),
                MakeLabelWithZIndex(4),
                MakeLabelWithZIndex(3),
                MakeLabelWithZIndex(2),
                MakeLabelWithZIndex(1),
            });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_List_Should_Be_With_ZIndex(bool isControlIdentifiers)
    {
        var deserializer = CreateDeserializer(isControlIdentifiers);

        using var yamlStream = File.OpenRead(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/With-list-of-controls-with-children.pa.yaml", isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        var result = deserializer.Deserialize<List<Control>>(yamlReader);

        result.Should().HaveCount(3);
        for (var i = 0; i < result.Count; i++)
        {
            result[i].Name.Should().Be($"Group{i + 1}");
            result[i].Children.Should().NotBeNull();
            for (var j = 0; j < result[i].Children!.Count; j++)
            {
                result[i].Children![j].Name.Should().Be($"Label{j + 1}");
                result[i].Children![j].ZIndex.Should().Be(result[i].Children!.Count - j);
            }
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_Container_List_Should_Be_With_ZIndex_Inverse(bool isControlIdentifiers)
    {
        var deserializer = CreateDeserializer(isControlIdentifiers);

        using var yamlStream = File.OpenRead(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/With-list-of-container-controls-with-children.pa.yaml", isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        var result = deserializer.Deserialize<List<Control>>(yamlReader);

        result.Should().HaveCount(3);
        for (var i = 0; i < result.Count; i++)
        {
            result[i].Name.Should().Be($"Group{i + 1}");
            result[i].Children.Should().NotBeNull();
            for (var j = 0; j < result[i].Children!.Count; j++)
            {
                result[i].Children![j].Name.Should().Be($"Label{j + 1}");
                result[i].Children![j].ZIndex.Should().Be(j + 1);
            }
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_List_Should_Ignore_ZIndex(bool isControlIdentifiers)
    {
        var deserializer = CreateDeserializer(isControlIdentifiers);

        using var yamlStream = File.OpenRead(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/with-existing-zindex.pa.yaml", isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        var result = deserializer.Deserialize<Screen>(yamlReader);
        if (result.Children == null)
        {
            Assert.Fail("Children should not be null");
            return;
        }
        result.Children.Should().HaveCount(5);
        for (var i = 0; i < result.Children.Count; i++)
        {
            result.Children[i].Name.Should().Be($"Label{result.Children.Count - i}");
            result.Children[i].ZIndex.Should().Be(result.Children.Count - i);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Seralizes_List_Should_Be_Without_ZIndex(bool isControlIdentifiers)
    {
        var graph = new List<Control>
        {
            MakeLabelWithZIndex(1),
            MakeLabelWithZIndex(3),
            MakeLabelWithZIndex(2),
            MakeLabelWithZIndex(5),
            MakeLabelWithZIndex(4),
        };

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.Serialize(graph);
        var expected = File.ReadAllText(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/With-list-of-controls.pa.yaml", isControlIdentifiers));
        sut.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Seralizes_SortedList_Should_Be_Without_ZIndex(bool isControlIdentifiers)
    {
        var graph = new SortedList<int, Control>
        {
            {1, MakeLabelWithZIndex(1) },
            {3, MakeLabelWithZIndex(3) },
            {2, MakeLabelWithZIndex(2) },
            {5, MakeLabelWithZIndex(5) },
            {4, MakeLabelWithZIndex(4) },
        };

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.Serialize(graph.Values);
        var expected = File.ReadAllText(GetTestFilePath(@"_TestData/ValidYaml{0}/ZIndexOrdering/With-sortedlist-of-controls.pa.yaml", isControlIdentifiers));
        sut.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Host_should_have_no_ZIndex(bool isControlIdentifiers)
    {
        var deserializer = CreateDeserializer(isControlIdentifiers);

        using var yamlStream = File.OpenRead(GetTestFilePath(@"_TestData/ValidYaml{0}/App.pa.yaml", isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        var result = deserializer.Deserialize<Control>(yamlReader);
        if (result.Children == null)
        {
            Assert.Fail("Children should not be null");
            return;
        }
        result.Children.Should().HaveCount(1);
        var host = result.Children[0];
        host.Name.Should().Be("Host");
        host.Properties.Should().NotContainKey(PropertyNames.ZIndex);
    }

    private Control MakeLabelWithZIndex(int i)
    {
        return ControlFactory.Create($"Label{i}", template: "text",
            properties:
            new()
            {
                { PropertyNames.ZIndex, i.ToString() },
            });
    }
}
