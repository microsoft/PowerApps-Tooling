// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Persistence.Tests.Extensions;

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
        var yaml = serializer.SerializeControl(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer(isTextFirst);

        var sut = deserializer.DeserializeControl<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.TemplateId.Should().Be("http://microsoft.com/appmagic/screen");
        sut.Children.Should().BeNull();
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
            properties: new()
            {
                { "Text", "I am a screen" },
            },
            children: new Control[]
            {
                ControlFactory.Create("Label1", template: "text",
                    properties:
                    new ()
                    {
                        { "Text", "lorem ipsum" },
                    }),
                ControlFactory.Create("Button1", template: "button",
                    properties : new ()
                    {
                        { "Text", "click me" },
                        { "X", "100" },
                        { "Y", "200" }
                    })
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.SerializeControl(graph.BeforeSerialize());

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.DeserializeControl<Control>(yaml).AfterDeserialize(ControlFactory);
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
            properties: new()
            {
                { "Text", "I am a custom control" },
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.SerializeControl(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.DeserializeControl<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<CustomControl>();
        sut.Name.Should().Be("CustomControl1");
        sut.TemplateId.Should().Be("http://localhost/#customcontrol");
        sut.Children.Should().BeNull();
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
        var yaml = serializer.SerializeControl(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.DeserializeControl<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<BuiltInControl>();
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-controls.pa.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml/Screen-with-name.pa.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/Control-with-custom-template.pa.yaml", typeof(CustomControl), "http://localhost/#customcontrol", "My Power Apps Custom Control", 0, 8)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-template-id.pa.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-template-name.pa.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-template.pa.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template", 0, 1)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-template-id.pa.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template id", 0, 1)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-template-name.pa.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template name", 0, 1)]
    public void Deserialize_ShouldSucceed(string path, Type expectedType, string expectedTemplateId, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.DeserializeControl(yamlReader, expectedType);

        // Assert
        controlObj.Should().BeAssignableTo(expectedType);
        var control = controlObj as Control;
        control!.TemplateId.Should().NotBeNull().And.Be(expectedTemplateId);
        control!.Name.Should().NotBeNull().And.Be(expectedName);
        if (controlCount > 0)
            control.Children.Should().NotBeNull().And.HaveCount(controlCount);
        else
            control.Children.Should().BeNull();
        control.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/App.pa.yaml", "Test app 1", 1, 0)]
    public void Deserialize_App_ShouldSucceed(string path, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var app = deserializer.DeserializeControl<App>(yamlReader);

        app!.Name.Should().NotBeNull().And.Be(expectedName);
        app.Children.Should().NotBeNull().And.HaveCount(controlCount);
        app.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }


    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/App-with-settings.pa.yaml", "Test App Name", 1)]
    public void Deserialize_App_WithSettings_ShouldSucceed(string path, string expectedName, int propertiesCount)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var app = deserializer.DeserializeControl<App>(yamlReader);

        app.Settings.Should().NotBeNull();
        app.Settings!.Name.Should().NotBeNull().And.Be(expectedName);
        app.Settings!.Layout.Should().Be(Settings.AppLayout.Landscape);
        app.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-unmatched-field.pa.yaml")]
    public void Deserialize_ShouldIgnoreUnmatchedProperties(string path)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.DeserializeControl<Screen>(yamlReader);

        // Assert
        controlObj.Should().NotBeNull();
    }

    [TestMethod]
    public void Deserialize_Strings()
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(@"_TestData/ValidYaml/Strings.pa.yaml");
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.DeserializeControl<BuiltInControl>(yamlReader);

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
    [DataRow(@"_TestData/ValidYaml/Component.pa.yaml", "MyCustomComponent", "Component", "http://microsoft.com/appmagic/Component")]
    [DataRow(@"_TestData/ValidYaml/CommandComponent.pa.yaml", "MyCustomCommandComponent", "CommandComponent", "http://microsoft.com/appmagic/CommandComponent")]
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
        var component = deserializer.DeserializeControl<Component>(yamlReader);

        // Assert
        component.Name.Should().Be(expectedName);
        component.Template.Should().NotBeNull();
        component.Template!.Name.Should().Be(expectedTemplateName);
        component.Template.Id.Should().Be(expectedTemplateId);
    }


    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-variant.pa.yaml", "built in", "Button", "SuperButton")]
    public void Variant_ShouldSucceed(
        string path,
        string expectedName,
        string expectedTemplateName,
        string expectedVariant)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var control = deserializer.DeserializeControl<BuiltInControl>(yamlReader);

        // Assert
        control.Name.Should().Be(expectedName);
        control.Template.Should().NotBeNull();
        control.Template!.Name.Should().Be(expectedTemplateName);
        control.Variant.Should().Be(expectedVariant);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen/with-gallery.pa.yaml")]
    public void Deserialize_Should_AddGalleryTemplate(string path)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var screen = deserializer.DeserializeControl<Screen>(yamlReader).AfterDeserialize(ControlFactory);

        // Assert
        screen.Should().NotBeNull();
        if (screen.Children == null)
            throw new ArgumentNullException(nameof(screen.Children));
        screen.Children.Should().NotBeNull().And.HaveCount(1);
        var gallery = screen.Children[0];
        gallery.Should().NotBeNull().And.BeOfType<BuiltInControl>();
        gallery.Template.Name.Should().Be("Gallery");
        if (gallery.Children == null)
            throw new ArgumentNullException(nameof(gallery.Children));

        // Check properties got moved to the gallery template
        gallery.Children.Should().HaveCount(2);
        gallery.Properties.Should().NotBeNull().And.HaveCount(2);
        gallery.Properties.Should().NotContainKeys("TemplateFill", "OnSelect");
        var galleryTemplate = gallery.Children.FirstOrDefault(c => c.Template.Name == "GalleryTemplate");
        if (galleryTemplate == null)
            throw new ArgumentNullException(nameof(galleryTemplate));
        galleryTemplate.Properties.Should().NotBeNull().And.HaveCount(1);
        galleryTemplate.Properties.Should().ContainKeys("TemplateFill");
    }

    public static IEnumerable<object[]> Deserialize_ShouldParseYamlForComponentCustomProperties_Data => new List<object[]>()
    {
        new object[]
        {
            new CustomProperty()
            {
                Name = "MyTextProp1",
                DataType = "String",
                Default = "lorem",
                Direction = CustomProperty.PropertyDirection.Input,
                Type = CustomProperty.PropertyType.Data,
                Parameters = Array.Empty<CustomPropertyParameter>()
            },
            @"_TestData/ValidYaml/Components/CustomProperty1.pa.yaml"
        },
        new object[]
        {
            new CustomProperty()
            {
                Name = "MyFuncProp1",
                DataType = "String",
                Default = "lorem",
                Direction = CustomProperty.PropertyDirection.Input,
                Type = CustomProperty.PropertyType.Function,
                Parameters = new[] {
                    new CustomPropertyParameter() { IsRequired = true, Name = "param1", DataType = "String" }
                },
            },
            @"_TestData/ValidYaml/Components/CustomProperty2.pa.yaml"
        }
    };

    [TestMethod]
    [DynamicData(nameof(Deserialize_ShouldParseYamlForComponentCustomProperties_Data))]
    public void Deserialize_ShouldParseYamlForComponentCustomProperties(CustomProperty expectedCustomProperty, string yamlFile)
    {
        var expectedYaml = File.ReadAllText(yamlFile);

        var sut = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var component = sut.DeserializeControl<Component>(expectedYaml);
        component.Should().NotBeNull();
        component!.CustomProperties.Should().NotBeNull().And.HaveCount(1);

        component.CustomProperties[0].Should().BeEquivalentTo(expectedCustomProperty);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Group/with-two-children.pa.yaml", 2, "My Small Group")]
    [DataRow(@"_TestData/ValidYaml/Group/with-nested-children.pa.yaml", 2, "My Nested Group")]
    public void Deserialize_ShouldParseYamlForGroupWithChildren(string path, int expectedChildrenCount, string expectedName)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var group = deserializer.DeserializeControl<GroupControl>(yamlReader);

        // Assert
        group.Should().NotBeNull();
        group.Children.Should().NotBeNull().And.HaveCount(expectedChildrenCount);
        group.Name.Should().Be(expectedName);
    }
}
