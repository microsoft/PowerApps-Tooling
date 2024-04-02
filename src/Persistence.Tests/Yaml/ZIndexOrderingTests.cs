// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Yaml;

[TestClass]
public class ZIndexOrderingTests : TestBase
{
    [TestMethod]
    public void SerializedChildrenInZOrder()
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

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph);
        var expected = File.ReadAllText(@"_TestData/ValidYaml/ZIndexOrdering/Screen-with-sorted-children.pa.yaml");
        sut.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void DeserializeChildrenShouldHaveMatchingZIndexProperty()
    {
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        using var yamlStream = File.OpenRead(@"_TestData/ValidYaml/ZIndexOrdering/Screen-with-sorted-children.pa.yaml");
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
    public void Deserialize_List_Should_Be_With_ZIndex()
    {
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        using var yamlStream = File.OpenRead(@"_TestData/ValidYaml/ZIndexOrdering/With-list-of-controls-with-children.pa.yaml");
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
    public void Seralizes_List_Should_Be_Without_ZIndex()
    {
        var graph = new List<Control>
        {
            MakeLabelWithZIndex(1),
            MakeLabelWithZIndex(3),
            MakeLabelWithZIndex(2),
            MakeLabelWithZIndex(5),
            MakeLabelWithZIndex(4),
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph);
        var expected = File.ReadAllText(@"_TestData/ValidYaml/ZIndexOrdering/With-list-of-controls.pa.yaml");
        sut.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void Seralizes_SortedList_Should_Be_Without_ZIndex()
    {
        var graph = new SortedList<int, Control>
        {
            {1, MakeLabelWithZIndex(1) },
            {3, MakeLabelWithZIndex(3) },
            {2, MakeLabelWithZIndex(2) },
            {5, MakeLabelWithZIndex(5) },
            {4, MakeLabelWithZIndex(4) },
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph.Values);
        var expected = File.ReadAllText(@"_TestData/ValidYaml/ZIndexOrdering/With-sortedlist-of-controls.pa.yaml");
        sut.Should().BeEquivalentTo(expected);
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
