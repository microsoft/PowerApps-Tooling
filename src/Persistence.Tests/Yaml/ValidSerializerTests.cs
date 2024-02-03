// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Persistence.Tests.Extensions;

namespace Persistence.Tests.Yaml;

[TestClass]
public class ValidSerializerTests : TestBase
{
    [TestMethod]
    public void Serialize_ShouldCreateValidYamlForSimpleStructure()
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" } },
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a screen{Environment.NewLine}");
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/App.fx.yaml")]
    public void Serialize_ShouldCreateValidYamlForApp(string expectedPath)
    {
        var app = ControlFactory.CreateApp("Test app 1");

        app.Screens = new Screen[]
        {
            ControlFactory.CreateScreen("Screen1",
                properties: new Dictionary<string, ControlPropertyValue>()
                {
                    { "Text", new() { Value = "I am a screen" } },
                }),
            ControlFactory.CreateScreen("Screen2",
                properties: new Dictionary<string, ControlPropertyValue>()
                {
                    { "Text", new() { Value = "I am another screen" } },
                }),
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(app).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    public void Serialize_ShouldSortControlPropertiesAlphabetically()
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "PropertyB", new() { Value = "B" } },
                { "PropertyC", new() { Value = "C" } },
                { "PropertyA", new() { Value = "A" } },
            });

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  PropertyA: A{Environment.NewLine}  PropertyB: B{Environment.NewLine}  PropertyC: C{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlWithChildNodes()
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" }  },
            },
            controls: new Control[]
            {
                ControlFactory.Create("Label1", template: "text",
                    properties: new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "lorem ipsum" }  },
                    }
                ),
                ControlFactory.Create("Button1", template: "button",
                    properties: new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "click me" }  },
                        { "X", new() { Value = "100" } },
                        { "Y", new() { Value = "200" } }
                    }
                )
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a screen{Environment.NewLine}Controls:{Environment.NewLine}- Text: {Environment.NewLine}  Name: Label1{Environment.NewLine}  Properties:{Environment.NewLine}    Text: lorem ipsum{Environment.NewLine}- Button: {Environment.NewLine}  Name: Button1{Environment.NewLine}  Properties:{Environment.NewLine}    Text: click me{Environment.NewLine}    X: 100{Environment.NewLine}    Y: 200{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlForCustomControl()
    {
        var graph = ControlFactory.Create("CustomControl1", template: "http://localhost/#customcontrol",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a custom control" } }
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph);
        sut.Should().Be($"Control: http://localhost/#customcontrol{Environment.NewLine}Name: CustomControl1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a custom control{Environment.NewLine}");
    }

    [TestMethod]
    [DataRow("ButtonCanvas", @"$""Interpolated text {User().FullName}""", @"_TestData/ValidYaml/Screen-with-BuiltInControl1.yaml")]
    [DataRow("ButtonCanvas", @"Normal text", @"_TestData/ValidYaml/Screen-with-BuiltInControl2.yaml")]
    [DataRow("ButtonCanvas", @"Text`~!@#$%^&*()_-+="", "":", @"_TestData/ValidYaml/Screen-with-BuiltInControl3.yaml")]
    [DataRow("ButtonCanvas", @"Hello : World", @"_TestData/ValidYaml/Screen-with-BuiltInControl4.yaml")]
    [DataRow("ButtonCanvas", @"Hello # World", @"_TestData/ValidYaml/Screen-with-BuiltInControl5.yaml")]
    [DataRow("ButtonCanvas", @"'Hello single quoted'", @"_TestData/ValidYaml/Screen-with-BuiltInControl6.yaml")]
    public void Serialize_ShouldCreateValidYaml_ForBuiltInControl(string templateName, string controlText, string expectedPath)
    {
        var graph = ControlFactory.Create("BuiltIn Control1", template: templateName,
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = controlText } }
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }
}
