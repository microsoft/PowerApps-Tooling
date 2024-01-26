// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Tests.Yaml;

[TestClass]
public class YamlSerializerTests
{
    [TestMethod]
    public void Serialize_ShouldCreateValidYamlForSimpleStructure()
    {
        var graph = new Screen()
        {
            Name = "Screen1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" } },
            },
        };

        var serializer = YamlSerializationFactory.CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a screen{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldSortControlPropertiesAlphabetically()
    {
        var graph = new Screen()
        {
            Name = "Screen1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "PropertyB", new() { Value = "B" } },
                { "PropertyC", new() { Value = "C" } },
                { "PropertyA", new() { Value = "A" } },
            },
        };

        var serializer = YamlSerializationFactory.CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  PropertyA: A{Environment.NewLine}  PropertyB: B{Environment.NewLine}  PropertyC: C{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlWithChildNodes()
    {
        var graph = new Screen()
        {
            Name = "Screen1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" }  },
            },
            Controls = new Control[]
            {
                new Text()
                {
                    Name = "Label1",
                    Properties = new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "lorem ipsum" }  },
                    },
                },
                new Button()
                {
                    Name = "Button1",
                    Properties = new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "click me" }  },
                        { "X", new() { Value = "100" } },
                        { "Y", new() { Value = "200" } }
                    },
                }
            }
        };

        var serializer = YamlSerializationFactory.CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a screen{Environment.NewLine}Controls:{Environment.NewLine}- Text: {Environment.NewLine}  Name: Label1{Environment.NewLine}  Properties:{Environment.NewLine}    Text: lorem ipsum{Environment.NewLine}- Button: {Environment.NewLine}  Name: Button1{Environment.NewLine}  Properties:{Environment.NewLine}    Text: click me{Environment.NewLine}    X: 100{Environment.NewLine}    Y: 200{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlForCustomControl()
    {
        var graph = new CustomControl()
        {
            ControlUri = "http://localhost/#customcontrol",
            Name = "CustomControl1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a custom control" } },
            },
        };

        var serializer = YamlSerializationFactory.CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Control: http://localhost/#customcontrol{Environment.NewLine}Name: CustomControl1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a custom control{Environment.NewLine}");
    }
}
