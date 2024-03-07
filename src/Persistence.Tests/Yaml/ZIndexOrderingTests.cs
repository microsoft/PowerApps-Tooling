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

        var sut = serializer.Serialize(graph);
        var expected = File.ReadAllText(@"_TestData/ValidYaml/ZIndexOrdering/Screen-with-sorted-children.fx.yaml");
        sut.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void DeserializeChildrenShouldHaveMatchingZIndexProperty()
    {
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        using var yamlStream = File.OpenRead(@"_TestData/ValidYaml/ZIndexOrdering/Screen-with-sorted-children.fx.yaml");
        using var yamlReader = new StreamReader(yamlStream);

        var sut = deserializer.Deserialize<Screen>(yamlReader);

        sut.Children.Should()
            .HaveCount(5)
            .And.BeEquivalentTo(new[]
            {
                MakeLabelWithZIndex(5),
                MakeLabelWithZIndex(4),
                MakeLabelWithZIndex(3),
                MakeLabelWithZIndex(2),
                MakeLabelWithZIndex(1),
            });
    }

    private Control MakeLabelWithZIndex(int i)
    {
        return ControlFactory.Create($"Label{i}", template: "text",
            new Dictionary<string, ControlPropertyValue>()
            {
                { PropertyNames.ZIndex, new() {Value = i.ToString()} },
            });
    }
}
