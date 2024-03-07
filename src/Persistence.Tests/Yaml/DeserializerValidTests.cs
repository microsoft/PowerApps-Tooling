// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerValidTests : TestBase
{
    [TestMethod]
    [DataRow(false, "I am a screen with spaces", "42", "71")]
    [DataRow(true, "\"I am a screen with spaces\"", "42", "71")]
    [DataRow(true, "NoSpaces", "-50", "=70")]
    [DataRow(true, "Yaml : | > ", "", "  ")]
    [DataRow(true, "Text`~!@#$%^&*()_-+=", ":", "\"\"")]
    [DataRow(true, "Text[]{};':\",.<>?/\\|", "@", "")]
    [DataRow(false, "こんにちは", "#", "'")]
    [DataRow(true, "Cos'è questo?", "---", "33")]
    public void Deserialize_ShouldParseSimpleStructure(bool isTextFirst,
        string textValue, string xValue, string yValue)
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "Text", textValue },
                { "X", xValue },
                { "Y", yValue },
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer(isTextFirst);
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer(isTextFirst);

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.TemplateId.Should().Be("http://microsoft.com/appmagic/screen");
        sut.Children.Should().NotBeNull().And.BeEmpty();
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(3)
                .And.ContainKeys("Text", "X", "Y");
        sut.Properties["Text"].Value.Should().Be(textValue);
        sut.Properties["X"].Value.Should().Be(xValue);
        sut.Properties["Y"].Value.Should().Be(yValue);
    }

    [TestMethod]
    public void Deserialize_ShouldParseYamlWithChildNodes()
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" }  },
            },
            children: new Control[]
            {
                ControlFactory.Create("Label1", template: "text",
                    new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "lorem ipsum" }  },
                    }),
                ControlFactory.Create("Button1", template: "button",
                    new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "click me" }  },
                        { "X", new() { Value = "100" } },
                        { "Y", new() { Value = "200" } }
                    })
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.TemplateId.Should().Be("http://microsoft.com/appmagic/screen");
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("I am a screen");

        sut.Children.Should().NotBeNull().And.HaveCount(2);
        sut.Children![0].Should().BeOfType<BuiltInControl>();
        sut.Children![0].Name.Should().Be("Label1");
        sut.Children![0].TemplateId.Should().Be("http://microsoft.com/appmagic/text");
        sut.Children![0].Properties.Should().NotBeNull()
                .And.HaveCount(2)
                .And.ContainKeys("Text", PropertyNames.ZIndex);
        sut.Children![0].Properties["Text"].Value.Should().Be("lorem ipsum");

        sut.Children![1].Should().BeOfType<BuiltInControl>();
        sut.Children![1].Name.Should().Be("Button1");
        sut.Children![1].TemplateId.Should().Be("http://microsoft.com/appmagic/button");
        sut.Children![1].Properties.Should().NotBeNull()
                .And.HaveCount(4)
                .And.ContainKeys("Text", "X", "Y", PropertyNames.ZIndex);
        sut.Children![1].Properties["Text"].Value.Should().Be("click me");
        sut.Children![1].Properties["X"].Value.Should().Be("100");
        sut.Children![1].Properties["Y"].Value.Should().Be("200");
    }

    [TestMethod]
    public void Deserialize_ShouldParseYamlForCustomControl()
    {
        var graph = ControlFactory.Create("CustomControl1", template: "http://localhost/#customcontrol",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a custom control" } },
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<CustomControl>();
        sut.Name.Should().Be("CustomControl1");
        sut.TemplateId.Should().Be("http://localhost/#customcontrol");
        sut.Children.Should().NotBeNull().And.BeEmpty();
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("I am a custom control");
    }

    [TestMethod]
    [DataRow("ButtonCanvas", "BuiltIn Button")]
    [DataRow("TextCanvas", "Text control name")]
    public void Deserialize_ShouldParseBuiltInControlFromYamlCustomControl(string templateName, string controlName)
    {
        var graph = ControlFactory.Create(controlName, templateName);

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<BuiltInControl>();
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-controls.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml/Screen-with-name.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/Control-with-custom-template.yaml", typeof(CustomControl), "http://localhost/#customcontrol", "My Power Apps Custom Control", 0, 8)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-template-id.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-template-name.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-template.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template", 0, 1)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-template-id.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template id", 0, 1)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-template-name.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template name", 0, 1)]
    public void Deserialize_ShouldSucceed(string path, Type expectedType, string expectedTemplateId, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize(yamlReader);

        // Assert
        controlObj.Should().BeAssignableTo(expectedType);
        var control = controlObj as Control;
        control!.TemplateId.Should().NotBeNull().And.Be(expectedTemplateId);
        control!.Name.Should().NotBeNull().And.Be(expectedName);
        control.Children.Should().NotBeNull().And.HaveCount(controlCount);
        control.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/App.fx.yaml", "Test app 1", 1, 0)]
    public void Deserialize_App_ShouldSucceed(string path, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize(yamlReader);
        controlObj.Should().BeAssignableTo<App>();
        var app = controlObj as App;
        app!.Name.Should().NotBeNull().And.Be(expectedName);
        app.Children.Should().NotBeNull().And.HaveCount(controlCount);
        app.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-unmatched-field.fx.yaml")]
    public void Deserialize_ShouldIgnoreUnmatchedProperties(string path)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize(yamlReader);

        // Assert
        controlObj.Should().NotBeNull();
    }

    [TestMethod]
    public void Deserialize_Strings()
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(@"_TestData/ValidYaml/Strings.fx.yaml");
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize<BuiltInControl>(yamlReader);

        // Assert
        controlObj.Should().NotBeNull();
        controlObj.Properties.Should().NotBeNull().And.HaveCount(12);

        controlObj.Properties["NormalText"].Value.Should().Be("\"This is a normal text\"");
        controlObj.Properties["MultiLineString"].Value.Should().Be("\"This is a multi-line\nstring\"");
        controlObj.Properties["NothingString"].Value.Should().NotBeNull().And.Be("\"\"");
        controlObj.Properties["NullTilde"].Value.Should().NotBeNull().And.Be("\"\"");
        controlObj.Properties["NullAsString"].Value.Should().NotBeNull().And.Be("\"null\"");
        controlObj.Properties["NullString"].Value.Should().NotBeNull().And.Be("\"\"");
        controlObj.Properties["EmptyString"].Value.Should().NotBeNull().And.Be("\"\"");
        controlObj.Properties["WhiteSpaceString"].Value.Should().NotBeNull().And.Be("\" \"");
        controlObj.Properties["NormalTextAgain"].Value.Should().Be("\"This is a normal text\"");
        controlObj.Properties["StartsWithEquals"].Value.Should().Be("\"=This string starts with equals\"");
        controlObj.Properties["StartsWithEqualsMultiLine"].Value.Should().Be("\"=This is a multi-line\nstarts with equals\"");
        controlObj.Properties["Formula"].Value.Should().Be("1+1");
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Component.fx.yaml", "MyCustomComponent", "Component", "http://microsoft.com/appmagic/Component")]
    [DataRow(@"_TestData/ValidYaml/CommandComponent.fx.yaml", "MyCustomCommandComponent", "CommandComponent", "http://microsoft.com/appmagic/CommandComponent")]
    public void Deserialize_Component_ShouldSucceed(
        string path,
        string expectedName,
        string expectedTemplateName,
        string expectedTemplateId)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var result = deserializer.Deserialize(yamlReader);
        result.Should().BeAssignableTo<Component>();

        // Assert
        var component = (Component)result!;
        component.Name.Should().Be(expectedName);
        component.Template.Should().NotBeNull();
        component.Template!.Name.Should().Be(expectedTemplateName);
        component.Template.Id.Should().Be(expectedTemplateId);
    }


}
