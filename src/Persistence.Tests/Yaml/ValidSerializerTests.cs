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
            properties: new()
            {
                { "Text", "\"I am a screen\"" },
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a screen{Environment.NewLine}");
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/App.pa.yaml")]
    public void Serialize_ShouldCreateValidYamlForApp(string expectedPath)
    {
        var app = ControlFactory.CreateApp("Test app 1");

        app.Screens = new Screen[]
        {
            ControlFactory.CreateScreen("Screen1",
                properties: new()
                {
                    { "Text", "I am a screen" },
                }),
            ControlFactory.CreateScreen("Screen2",
                properties: new()
                {
                    { "Text", "I am another screen" },
                }),
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(app).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    public void Serialize_ShouldSortControlPropertiesAlphabetically()
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "PropertyB", "B" },
                { "PropertyC", "C" },
                { "PropertyA", "A" },
            });

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  PropertyA: =A{Environment.NewLine}  PropertyB: =B{Environment.NewLine}  PropertyC: =C{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlWithChildNodes()
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "Text", "\"I am a screen\"" },
            },
            children: new Control[]
            {
                ControlFactory.Create("Label1", template: "text",
                    properties: new()
                    {
                        { "Text", "\"lorem ipsum\"" },
                    }
                ),
                ControlFactory.Create("Button1", template: "button",
                    properties: new()
                    {
                        { "Text", "\"click me\"" },
                        { "X", "100" },
                        { "Y", "200" }
                    }
                )
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph);
        sut.Should().Be($"Screen: {Environment.NewLine}Name: Screen1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a screen{Environment.NewLine}Children:{Environment.NewLine}- Text: {Environment.NewLine}  Name: Label1{Environment.NewLine}  Properties:{Environment.NewLine}    Text: lorem ipsum{Environment.NewLine}- Button: {Environment.NewLine}  Name: Button1{Environment.NewLine}  Properties:{Environment.NewLine}    Text: click me{Environment.NewLine}    X: =100{Environment.NewLine}    Y: =200{Environment.NewLine}");
    }

    [TestMethod]
    public void Serialize_ShouldCreateValidYamlForCustomControl()
    {
        var graph = ControlFactory.Create("CustomControl1", template: "http://localhost/#customcontrol",
            properties: new()
            {
                { "Text", "\"I am a custom control\"" }
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph);
        sut.Should().Be($"Control: http://localhost/#customcontrol{Environment.NewLine}Name: CustomControl1{Environment.NewLine}Properties:{Environment.NewLine}  Text: I am a custom control{Environment.NewLine}");
    }

    [TestMethod]
    [DataRow("ButtonCanvas", "$\"Interpolated text {User().FullName}\"", @"_TestData/ValidYaml/BuiltInControl1.pa.yaml")]
    [DataRow("ButtonCanvas", "\"Normal text\"", @"_TestData/ValidYaml/BuiltInControl2.pa.yaml")]
    [DataRow("ButtonCanvas", "Text`~!@#$%^&*()_-+=\", \":", @"_TestData/ValidYaml/BuiltInControl3.pa.yaml")]
    [DataRow("ButtonCanvas", "\"Hello : World\"", @"_TestData/ValidYaml/BuiltInControl4.pa.yaml")]
    [DataRow("ButtonCanvas", "\"Hello # World\"", @"_TestData/ValidYaml/BuiltInControl5.pa.yaml")]
    [DataRow("ButtonCanvas", "'Hello single quoted'.Text", @"_TestData/ValidYaml/BuiltInControl6.pa.yaml")]
    [DataRow("ButtonCanvas", "\"=Starts with equals\"", @"_TestData/ValidYaml/BuiltInControl7.pa.yaml")]
    public void Serialize_ShouldCreateValidYaml_ForBuiltInControl(string templateName, string controlText, string expectedPath)
    {
        var graph = ControlFactory.Create("BuiltIn Control1", template: templateName,
            properties: new()
            {
                { "Text", controlText }
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen/with-gallery.pa.yaml")]
    public void Serialize_Should_FlattenGalleryTemplate(string expectedPath)
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "Text", "\"I am a screen\"" },
            },
            children: new Control[]
            {
                ControlFactory.Create("Gallery1", template: "gallery",
                    properties: new()
                    {
                        { "Items", "Accounts" },
                    },
                    children: new List<Control>()
                    {
                        ControlFactory.Create("galleryTemplate1", template: "galleryTemplate",
                            properties: new()
                            {
                                { "TemplateFill", "RGBA(0, 0, 0, 0)" },
                            }
                        ),
                        ControlFactory.Create("button12", template: "button",
                            properties: new()
                            {
                                { "Fill", "RGBA(0, 0, 0, 0)" },
                            }
                        )
                    }
                )
            }
        );

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-variant.pa.yaml", "SuperButton", null)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-layout.pa.yaml", "SuperButton", "vertical")]
    public void Valid_Variant(string expectedPath, string expectedVariant, string expectedLayout)
    {
        var graph = ControlFactory.Create("built in", "Button", variant: expectedVariant,
            properties: new()
            {
                { "Text", "\"button text\"" },
            }
        );
        graph.Layout = expectedLayout;

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl/with-sorted-properties.pa.yaml", "SuperButton", "Some value")]
    public void Should_SortBy_Category(string expectedPath, string templateName, string expectedValue)
    {
        var graph = ControlFactory.Create("BuiltIn Control1", template: templateName,
            properties: new Dictionary<string, ControlProperty>()
            {
                { "a2_Data", new("a2_Data", expectedValue) { Category = PropertyCategory.Data } },
                { "a1_Data", new("a1_Data", expectedValue) { Category = PropertyCategory.Data } },
                { "x2_Behavior", new("x2_Behavior", expectedValue) { Category = PropertyCategory.Behavior } },
                { "x1_Behavior", new("x1_Behavior", expectedValue) { Category = PropertyCategory.Behavior } },
                { "y3_Design", new("y3_Design", expectedValue) { Category = PropertyCategory.Design } },
                { "y2_Design", new("y2_Design", expectedValue) { Category = PropertyCategory.Design } },
                { "y1_Design", new("y1_Design", expectedValue) { Category = PropertyCategory.Design } },
            }
        );
        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }


    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/With-list-of-controls.pa.yaml", "SuperButton")]
    public void Should_Serialize_List_Of_Controls(string expectedPath, string templateName)
    {
        var graph = new List<Control>()
        {
            ControlFactory.Create("BuiltIn Control1", template: templateName,
                properties: new()
                {
                    { "Text", "Just text" },
                }
            ),
            ControlFactory.Create("BuiltIn Label", template: "Label",
                properties: new()
                {
                    { "Text", "Just label text" },
                }
            ),
        };
        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var sut = serializer.Serialize(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedPath).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }


    public static IEnumerable<object[]> Serialize_ShouldCreateValidYamlForComponentCustomProperties_Data => new List<object[]>()
    {
        new object[]
        {
            new CustomProperty()
            {
                Name = "MyTextProp1",
                DataType = "String",
                Default = "lorem",
                Direction = CustomProperty.PropertyDirection.Input,
                Type = CustomProperty.PropertyType.Data
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
                    new CustomPropertyParameter(){
                        Name = "param1",
                        DataType= "String",
                        IsRequired = true,
                    }
                },
            },
            @"_TestData/ValidYaml/Components/CustomProperty2.pa.yaml"
        }
    };

    [TestMethod]
    [DynamicData(nameof(Serialize_ShouldCreateValidYamlForComponentCustomProperties_Data))]
    public void Serialize_ShouldCreateValidYamlForComponentCustomProperties(CustomProperty customProperty, string expectedYamlFile)
    {
        var component = ControlFactory.Create("Component1", "Component") as Component;
        component.Should().NotBeNull();
        component!.CustomProperties.Should().NotBeNull();
        component.CustomProperties.Add(customProperty.Name, customProperty);

        var sut = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();

        var serializedComponent = sut.SerializeControl(component);

        var expectedYaml = File.ReadAllText(expectedYamlFile);
        serializedComponent.Should().Be(expectedYaml);
    }
}
