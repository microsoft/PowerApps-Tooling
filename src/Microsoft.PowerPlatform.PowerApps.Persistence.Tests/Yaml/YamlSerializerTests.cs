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
        sut.Should().Be("Screen: \nName: Screen1\nProperties:\n  Text: I am a screen\n");
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
                new Label()
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
        sut.Should().Be("Screen: \nName: Screen1\nProperties:\n  Text: I am a screen\nControls:\n- Label: \n  Name: Label1\n  Properties:\n    Text: lorem ipsum\n- Button: \n  Name: Button1\n  Properties:\n    Text: click me\n    X: 100\n    Y: 200\n");
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
        sut.Should().Be("Control: http://localhost/#customcontrol\nName: CustomControl1\nProperties:\n  Text: I am a custom control\n");
    }
}
