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
            Properties = new Dictionary<string, string>()
            {
                { "Text", "I am a screen" },
            },
        };

        var sut = YamlSerializerFactory.Create();
        var yaml = sut.Serialize(graph);
        yaml.Should().Be("Screen: \r\nName: Screen1\r\nProperties:\r\n  Text: I am a screen\r\n");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlWithChildNodes()
    {
        var graph = new Screen()
        {
            Name = "Screen1",
            Properties = new Dictionary<string, string>()
            {
                { "Text", "I am a screen" },
            },
            Controls = new Control[]
            {
                new Label()
                {
                    Name = "Label1",
                    Properties = new Dictionary<string, string>()
                    {
                        { "Text", "lorem ipsum" },
                    },
                },
                new Button()
                {
                    Name = "Button1",
                    Properties = new Dictionary<string, string>()
                    {
                        { "Text", "click me" },
                        { "X", "100" },
                        { "Y", "200"}
                    },
                }
            }
        };

        var sut = YamlSerializerFactory.Create();
        var yaml = sut.Serialize(graph);
        yaml.Should().Be("Screen: \r\nName: Screen1\r\nProperties:\r\n  Text: I am a screen\r\nControls:\r\n- Label: \r\n  Name: Label1\r\n  Properties:\r\n    Text: lorem ipsum\r\n- Button: \r\n  Name: Button1\r\n  Properties:\r\n    Text: click me\r\n    X: 100\r\n    Y: 200\r\n");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlForCustomControl()
    {
        var graph = new CustomControl()
        {
            ControlUri = "http://localhost/#customcontrol",
            Name = "CustomControl1",
            Properties = new Dictionary<string, string>()
            {
                { "Text", "I am a custom control" },
            },
        };

        var sut = YamlSerializerFactory.Create();
        var yaml = sut.Serialize(graph);
        yaml.Should().Be("Control: http://localhost/#customcontrol\r\nName: CustomControl1\r\nProperties:\r\n  Text: I am a custom control\r\n");
    }
}
