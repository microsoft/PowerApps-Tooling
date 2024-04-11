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

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer(new() { IsTextFirst = isTextFirst });
        var yaml = serializer.SerializeControl(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer(new() { IsTextFirst = true });

        var sut = deserializer.Deserialize<Control>(yaml);
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
                { "Text", "\"I am a screen\"" },
            },
            children: new Control[]
            {
                ControlFactory.Create("Label1", template: "text",
                    properties:
                    new ()
                    {
                        { "Text", "\"lorem ipsum\"" },
                    }),
                ControlFactory.Create("Button1", template: "button",
                    properties : new ()
                    {
                        { "Text", "\"click me\"" },
                        { "X", "100" },
                        { "Y", "200" }
                    })
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.SerializeControl(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.TemplateId.Should().Be("http://microsoft.com/appmagic/screen");
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("\"I am a screen\"");

        sut.Children.Should().NotBeNull().And.HaveCount(2);
        sut.Children![0].Should().BeOfType<BuiltInControl>();
        sut.Children![0].Name.Should().Be("Label1");
        sut.Children![0].TemplateId.Should().Be("http://microsoft.com/appmagic/text");
        sut.Children![0].Properties.Should().NotBeNull()
                .And.HaveCount(2)
                .And.ContainKeys("Text", PropertyNames.ZIndex);
        sut.Children![0].Properties["Text"].Value.Should().Be("\"lorem ipsum\"");
        sut.Children![0].Properties[PropertyNames.ZIndex].Value.Should().Be("2");

        sut.Children![1].Should().BeOfType<BuiltInControl>();
        sut.Children![1].Name.Should().Be("Button1");
        sut.Children![1].TemplateId.Should().Be("http://microsoft.com/appmagic/button");
        sut.Children![1].Properties.Should().NotBeNull()
                .And.HaveCount(4)
                .And.ContainKeys("Text", "X", "Y", PropertyNames.ZIndex);
        sut.Children![1].Properties["Text"].Value.Should().Be("\"click me\"");
        sut.Children![1].Properties["X"].Value.Should().Be("100");
        sut.Children![1].Properties["Y"].Value.Should().Be("200");
        sut.Children![1].Properties[PropertyNames.ZIndex].Value.Should().Be("1");
    }


    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Deserialize_GroupContainersShouldOrderZIndexInverse(bool isControlIdentifiers)
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "Text", "\"I am a screen\"" },
            },
            children: new Control[]
            {
                ControlFactory.Create("group", template: "groupContainer", children: new Control[] {
                    ControlFactory.Create("Label1", template: "text",
                        properties:
                        new ()
                        {
                            { "Text", "\"lorem ipsum\"" },
                        }),
                    ControlFactory.Create("Button1", template: "button",
                        properties : new ()
                        {
                            { "Text", "\"click me\"" },
                            { "X", "100" },
                            { "Y", "200" }
                        })
                })
            }
        );

        var serializer = CreateSerializer(isControlIdentifiers);
        var yaml = serializer.SerializeControl(graph);

        var deserializer = CreateDeserializer(isControlIdentifiers);

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.TemplateId.Should().Be("http://microsoft.com/appmagic/screen");
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("\"I am a screen\"");
        sut.Children.Should().NotBeNull().And.HaveCount(1);

        var group = sut.Children![0];

        group.Children.Should().NotBeNull().And.HaveCount(2);
        group.Children![0].Should().BeOfType<BuiltInControl>();
        group.Children![0].Name.Should().Be("Label1");
        group.Children![0].TemplateId.Should().Be("http://microsoft.com/appmagic/text");
        group.Children![0].Properties.Should().NotBeNull()
                .And.HaveCount(2)
                .And.ContainKeys("Text", PropertyNames.ZIndex);
        group.Children![0].Properties["Text"].Value.Should().Be("\"lorem ipsum\"");
        group.Children![0].Properties[PropertyNames.ZIndex].Value.Should().Be("1");

        group.Children![1].Should().BeOfType<BuiltInControl>();
        group.Children![1].Name.Should().Be("Button1");
        group.Children![1].TemplateId.Should().Be("http://microsoft.com/appmagic/button");
        group.Children![1].Properties.Should().NotBeNull()
                .And.HaveCount(4)
                .And.ContainKeys("Text", "X", "Y", PropertyNames.ZIndex);
        group.Children![1].Properties["Text"].Value.Should().Be("\"click me\"");
        group.Children![1].Properties["X"].Value.Should().Be("100");
        group.Children![1].Properties[PropertyNames.ZIndex].Value.Should().Be("2");
    }

    [TestMethod]
    public void Deserialize_ShouldParseYamlForCustomControl()
    {
        var graph = ControlFactory.Create("CustomControl1", template: "http://localhost/#customcontrol",
            properties: new()
            {
                { "Text", "\"I am a custom control\"" },
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.SerializeControl(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<CustomControl>();
        sut.Name.Should().Be("CustomControl1");
        sut.TemplateId.Should().Be("http://localhost/#customcontrol");
        sut.Children.Should().BeNull();
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("\"I am a custom control\"");
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

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<BuiltInControl>();
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-controls.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-controls.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-name.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen", "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-name.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen", "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Control-with-custom-template.pa.yaml", true, typeof(CustomControl), "http://localhost/#customcontrol", "My Power Apps Custom Control", 0, 8)]
    [DataRow(@"_TestData/ValidYaml{0}/Control-with-custom-template.pa.yaml", false, typeof(CustomControl), "http://localhost/#customcontrol", "My Power Apps Custom Control", 0, 8)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-template-id.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-template-id.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-template-name.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-template-name.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-template.pa.yaml", true, typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template", 0, 1)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-template.pa.yaml", false, typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template", 0, 1)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-template-id.pa.yaml", true, typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template id", 0, 1)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-template-id.pa.yaml", false, typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template id", 0, 1)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-template-name.pa.yaml", true, typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template name", 0, 1)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-template-name.pa.yaml", false, typeof(BuiltInControl), "http://microsoft.com/appmagic/button", "button with template name", 0, 1)]
    public void Deserialize_ShouldSucceed(string path, bool isControlIdentifiers, Type expectedType, string expectedTemplateId, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize<Control>(yamlReader);

        // Assert
        controlObj.Should().BeAssignableTo(expectedType);
        controlObj!.TemplateId.Should().NotBeNull().And.Be(expectedTemplateId);
        controlObj!.Name.Should().NotBeNull().And.Be(expectedName);
        if (controlCount > 0)
            controlObj.Children.Should().NotBeNull().And.HaveCount(controlCount);
        else
            controlObj.Children.Should().BeNull();
        controlObj.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/App.pa.yaml", true, "Test app 1", 1, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/App.pa.yaml", false, "Test app 1", 1, 0)]
    public void Deserialize_App_ShouldSucceed(string path, bool isControlIdentifiers, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var app = deserializer.Deserialize<Control>(yamlReader);

        app!.Name.Should().NotBeNull().And.Be(expectedName);
        app.Children.Should().NotBeNull().And.HaveCount(controlCount);
        app.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }


    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/App-with-settings.pa.yaml", true, "Test App Name", 1)]
    [DataRow(@"_TestData/ValidYaml{0}/App-with-settings.pa.yaml", false, "Test App Name", 1)]
    public void Deserialize_App_WithSettings_ShouldSucceed(string path, bool isControlIdentifiers, string expectedName, int propertiesCount)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var app = deserializer.Deserialize<App>(yamlReader);
        if (app == null)
            throw new InvalidOperationException(nameof(app));

        app.Settings.Should().NotBeNull();
        app.Settings!.Name.Should().NotBeNull().And.Be(expectedName);
        app.Settings!.Layout.Should().Be(Settings.AppLayout.Landscape);
        app.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-unmatched-field.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-unmatched-field.pa.yaml", false)]
    public void Deserialize_ShouldIgnoreUnmatchedProperties(string path, bool isControlIdentifiers)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize<Screen>(yamlReader);

        // Assert
        controlObj.Should().NotBeNull();
    }

    [TestMethod]
    public void Deserialize_Strings()
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer
        (
            new YamlSerializationOptions() { IsTextFirst = true }
        );
        using var yamlStream = File.OpenRead(@"_TestData/ValidYaml/Strings.pa.yaml");
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
    [DataRow(@"_TestData/ValidYaml{0}/Component.pa.yaml", true, "MyCustomComponent", "Component", "http://microsoft.com/appmagic/Component", "lorem ipsum", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Component.pa.yaml", false, "MyCustomComponent", "Component", "http://microsoft.com/appmagic/Component", "lorem ipsum", true)]
    [DataRow(@"_TestData/ValidYaml{0}/CommandComponent.pa.yaml", true, "MyCustomCommandComponent", "CommandComponent", "http://microsoft.com/appmagic/CommandComponent", "lorem ipsum", true)]
    [DataRow(@"_TestData/ValidYaml{0}/CommandComponent.pa.yaml", false, "MyCustomCommandComponent", "CommandComponent", "http://microsoft.com/appmagic/CommandComponent", "lorem ipsum", true)]
    public void Deserialize_Component_ShouldSucceed(
        string path, bool isControlIdentifiers,
        string expectedName,
        string expectedTemplateName,
        string expectedTemplateId,
        string expectedDescription,
        bool expectedAccessAppScope)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var component = deserializer.Deserialize<Component>(yamlReader);

        // Assert
        component.Name.Should().Be(expectedName);
        component.Description.Should().Be(expectedDescription);
        component.AccessAppScope.Should().Be(expectedAccessAppScope);
        component.Template.Should().NotBeNull();
        component.Template!.Name.Should().Be(expectedTemplateName);
        component.Template.Id.Should().Be(expectedTemplateId);
    }


    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-variant.pa.yaml", true, "built in", "Button", "SuperButton")]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-variant.pa.yaml", false, "built in", "Button", "SuperButton")]
    public void Variant_ShouldSucceed(
        string path, bool isControlIdentifiers,
        string expectedName,
        string expectedTemplateName,
        string expectedVariant)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var control = deserializer.Deserialize<BuiltInControl>(yamlReader);

        // Assert
        control.Name.Should().Be(expectedName);
        control.Template.Should().NotBeNull();
        control.Template!.Name.Should().Be(expectedTemplateName);
        control.Variant.Should().Be(expectedVariant);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-gallery.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-gallery.pa.yaml", false)]
    public void Deserialize_Should_AddGalleryTemplate(string path, bool isControlIdentifiers)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var screen = deserializer.Deserialize<Control>(yamlReader);

        // Assert
        screen.ShouldNotBeNull();
        screen.Children.ShouldNotBeNull();
        screen.Children.Should().NotBeNull().And.HaveCount(1);
        var gallery = screen.Children[0];
        gallery.Should().NotBeNull().And.BeOfType<BuiltInControl>();
        gallery.Template.Name.Should().Be("Gallery");
        gallery.Children.ShouldNotBeNull();

        // Check properties got moved to the gallery template
        gallery.Children.Should().HaveCount(2);
        gallery.Properties.Should().NotBeNull().And.HaveCount(2);
        gallery.Properties.Should().NotContainKeys("TemplateFill", "OnSelect");
        var galleryTemplate = gallery.Children.FirstOrDefault(c => c.Template.Name == "GalleryTemplate");
        galleryTemplate.ShouldNotBeNull();
        galleryTemplate.Properties.Should().NotBeNull().And.HaveCount(1);
        galleryTemplate.Properties.Should().ContainKeys("TemplateFill");
    }

    [TestMethod]
    [DynamicData(nameof(ComponentCustomProperties_Data), typeof(TestBase))]
    public void Deserialize_ShouldParseYamlForComponentCustomProperties(CustomProperty[] expectedCustomProperties, string yamlFile, bool isControlIdentifiers)
    {
        var expectedYaml = File.ReadAllText(GetTestFilePath(yamlFile, isControlIdentifiers));
        var deserializer = CreateDeserializer(isControlIdentifiers);

        var component = deserializer.Deserialize<Component>(expectedYaml);
        component.Should().NotBeNull();
        component!.CustomProperties.Should().NotBeNull()
            .And.HaveCount(expectedCustomProperties.Length);

        for (var i = 0; i < expectedCustomProperties.Length; i++)
        {
            component.CustomProperties[i].Should().BeEquivalentTo(expectedCustomProperties[i]);
        }
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Group/with-two-children.pa.yaml", true, 2, "My Small Group")]
    [DataRow(@"_TestData/ValidYaml{0}/Group/with-two-children.pa.yaml", false, 2, "My Small Group")]
    [DataRow(@"_TestData/ValidYaml{0}/Group/with-nested-children.pa.yaml", true, 2, "My Nested Group")]
    [DataRow(@"_TestData/ValidYaml{0}/Group/with-nested-children.pa.yaml", false, 2, "My Nested Group")]
    public void Deserialize_ShouldParseYamlForGroupWithChildren(string path, bool isControlIdentifiers, int expectedChildrenCount, string expectedName)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var group = deserializer.Deserialize<GroupControl>(yamlReader);

        // Assert
        group.Should().NotBeNull();
        group.Children.Should().NotBeNull().And.HaveCount(expectedChildrenCount);
        group.Name.Should().Be(expectedName);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/With-list-of-controls.pa.yaml", true, 2, typeof(CustomControl), "BuiltIn Control1")]
    [DataRow(@"_TestData/ValidYaml{0}/With-list-of-controls.pa.yaml", false, 2, typeof(CustomControl), "BuiltIn Control1")]
    public void Deserialize_ShouldParse_Lists_of_Controls(string path, bool isControlIdentifiers, int expectedCount, Type expectedType, string expectedName)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var listObj = deserializer.Deserialize<object>(yamlReader);
        listObj.Should().NotBeNull().And.BeAssignableTo(typeof(List<Control>));
        var list = (List<Control>)listObj;
        list.Should().HaveCount(expectedCount);
        list.First().Should().BeOfType(expectedType);
        list.First().Name.Should().Be(expectedName);
    }
}
