// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Yaml;

[TestClass]
public class ValidSerializerTests : TestBase
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-name.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-name.pa.yaml", false)]
    public void Serialize_ShouldCreateValidYaml_for_Screen(string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.Create("Hello", "Screen");

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/App.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/App.pa.yaml", false)]
    public void Serialize_ShouldCreateValidYamlForApp(string expectedPath, bool isControlIdentifiers)
    {
        var app = ControlFactory.CreateApp("Test app 1");

        app.Screens = new Screen[]
        {
            ControlFactory.CreateScreen("Screen1",
                properties: new()
                {
                    { "Text", "\"I am a screen\"" },
                }),
            ControlFactory.CreateScreen("Screen2",
                properties: new()
                {
                    { "Text", "\"I am another screen\"" },
                }),
        };

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(app).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-not-sorted.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-not-sorted.pa.yaml", false)]
    public void Serialize_ShouldSortControlPropertiesAlphabetically(string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "PropertyB", "=B" },
                { "PropertyC", "=C" },
                { "PropertyA", "=A" },
            });

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-controls1.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-controls1.pa.yaml", false)]
    public void Serialize_ShouldCreateValidYamlWithChildNodes(string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.Create("Screen1", templateNameOrId: "Screen",
            properties: new()
            {
                { "Text", "\"I am a screen\"" },
            },
            children: new Control[]
            {
                ControlFactory.Create("Label1", templateNameOrId: "text", isClassic: true,
                    properties: new()
                    {
                        { "Text", "\"lorem ipsum\"" },
                    }
                ),
                ControlFactory.Create("Button1", templateNameOrId: "button", isClassic: true,
                    properties: new()
                    {
                        { "Text", "\"click me\"" },
                        { "X", "100" },
                        { "Y", "200" }
                    }
                )
            }
        );

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/CustomControls/with-property.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/CustomControls/with-property.pa.yaml", false)]
    public void Serialize_ShouldCreateValidYamlForCustomControl(string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.Create("CustomControl1", templateNameOrId: "http://localhost/#customcontrol",
            properties: new()
            {
                { "Text", "\"I am a custom control\"" }
            }
        );

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow("ButtonCanvas", "$\"Interpolated text {User().FullName}\"", @"_TestData/ValidYaml{0}/BuiltInControl1.pa.yaml", false)]
    [DataRow("ButtonCanvas", "$\"Interpolated text {User().FullName}\"", @"_TestData/ValidYaml{0}/BuiltInControl1.pa.yaml", true)]
    [DataRow("ButtonCanvas", "\"Normal text\"", @"_TestData/ValidYaml{0}/BuiltInControl2.pa.yaml", false)]
    [DataRow("ButtonCanvas", "\"Normal text\"", @"_TestData/ValidYaml{0}/BuiltInControl2.pa.yaml", true)]
    [DataRow("ButtonCanvas", "\"Text`~!@#$%^&*()_-+=\", \":\"", @"_TestData/ValidYaml{0}/BuiltInControl3.pa.yaml", false)]
    [DataRow("ButtonCanvas", "\"Text`~!@#$%^&*()_-+=\", \":\"", @"_TestData/ValidYaml{0}/BuiltInControl3.pa.yaml", true)]
    [DataRow("ButtonCanvas", "\"Hello : World\"", @"_TestData/ValidYaml{0}/BuiltInControl4.pa.yaml", false)]
    [DataRow("ButtonCanvas", "\"Hello : World\"", @"_TestData/ValidYaml{0}/BuiltInControl4.pa.yaml", true)]
    [DataRow("ButtonCanvas", "\"Hello # World\"", @"_TestData/ValidYaml{0}/BuiltInControl5.pa.yaml", false)]
    [DataRow("ButtonCanvas", "\"Hello # World\"", @"_TestData/ValidYaml{0}/BuiltInControl5.pa.yaml", true)]
    [DataRow("ButtonCanvas", "'Hello single quoted'.Text", @"_TestData/ValidYaml{0}/BuiltInControl6.pa.yaml", false)]
    [DataRow("ButtonCanvas", "'Hello single quoted'.Text", @"_TestData/ValidYaml{0}/BuiltInControl6.pa.yaml", true)]
    [DataRow("ButtonCanvas", "\"=Starts with equals\"", @"_TestData/ValidYaml{0}/BuiltInControl7.pa.yaml", false)]
    [DataRow("ButtonCanvas", "\"=Starts with equals\"", @"_TestData/ValidYaml{0}/BuiltInControl7.pa.yaml", true)]
    [DataRow("ButtonCanvas", "\"Text containing PFX \"\"Double-Double-Quote\"\" escape sequence\"", @"_TestData/ValidYaml{0}/BuiltInControl8.pa.yaml", false)]
    [DataRow("ButtonCanvas", "\"Text containing PFX \"\"Double-Double-Quote\"\" escape sequence\"", @"_TestData/ValidYaml{0}/BuiltInControl8.pa.yaml", true)]
    public void Serialize_ShouldCreateValidYaml_ForBuiltInControl(string templateName, string controlText, string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.Create("BuiltIn Control1", templateNameOrId: templateName,
            properties: new()
            {
                { "Text", controlText }
            }
        );

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-gallery.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-gallery.pa.yaml", false)]
    public void Serialize_Should_FlattenGalleryTemplate(string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.CreateScreen("Screen1",
            properties: new()
            {
                { "Text", "\"I am a screen\"" },
            },
            children: new Control[]
            {
                ControlFactory.Create("Gallery1", templateNameOrId: "gallery",
                    properties: new()
                    {
                        { "Items", "Accounts" },
                    },
                    children: new List<Control>()
                    {
                        ControlFactory.Create("galleryTemplate1", templateNameOrId: "galleryTemplate",
                            properties: new()
                            {
                                { "TemplateFill", "RGBA(0, 0, 0, 0)" },
                            }
                        ),
                        ControlFactory.Create("button12", templateNameOrId: "button",
                            properties: new()
                            {
                                { "Fill", "RGBA(0, 0, 0, 0)" },
                            }
                        )
                    }
                )
            }
        );

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-variant.pa.yaml", "SuperButton", null, true)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-variant.pa.yaml", "SuperButton", null, false)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-layout.pa.yaml", "SuperButton", "vertical", true)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-layout.pa.yaml", "SuperButton", "vertical", false)]
    public void Valid_Variant(string expectedPath, string expectedVariant, string expectedLayout, bool isControlIdentifiers)
    {
        var graph = ControlFactory.Create("built in", templateNameOrId: "Button", variant: expectedVariant,
            properties: new()
            {
                { "Text", "\"button text\"" },
            }
        );
        graph.Layout = expectedLayout;

        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-sorted-properties.pa.yaml", true, "SuperButton", "Some value")]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl/with-sorted-properties.pa.yaml", false, "SuperButton", "Some value")]
    public void Should_SortBy_Category(string expectedPath, bool isControlIdentifiers, string templateName, string expectedValue)
    {
        var graph = ControlFactory.Create("BuiltIn Control1", templateNameOrId: templateName,
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
        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.SerializeControl(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }


    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/With-list-of-controls.pa.yaml", true, "SuperButton")]
    [DataRow(@"_TestData/ValidYaml{0}/With-list-of-controls.pa.yaml", false, "SuperButton")]
    public void Should_Serialize_List_Of_Controls(string expectedPath, bool isControlIdentifiers, string templateName)
    {
        var graph = new List<Control>()
        {
            ControlFactory.Create("BuiltIn Control1", templateNameOrId: templateName,
                properties: new()
                {
                    { "Text", "Just text" },
                }
            ),
            ControlFactory.Create("BuiltIn Label", templateNameOrId: "Label",
                properties: new()
                {
                    { "Text", "Just label text" },
                }
            ),
        };
        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.Serialize(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DynamicData(nameof(ComponentCustomProperties_Data), typeof(TestBase))]
    public void Serialize_ShouldCreateValidYamlForComponentCustomProperties(CustomProperty[] customProperties, string expectedYamlFile, bool isControlIdentifiers)
    {
        var component = ControlFactory.Create("Component1", "Component") as ComponentDefinition;
        component.Should().NotBeNull();
        component!.CustomProperties.Should().NotBeNull();
        foreach (var prop in customProperties)
        {
            component.CustomProperties.Add(prop);
        }

        var sut = CreateSerializer(isControlIdentifiers);

        var serializedComponent = sut.SerializeControl(component).NormalizeNewlines();

        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedYamlFile, isControlIdentifiers)).NormalizeNewlines();
        serializedComponent.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(ComponentType.Canvas, "lorem ipsum dolor", false, "Component1:\n  Control: Component\n  Description: lorem ipsum dolor\n")]
    [DataRow(ComponentType.Canvas, "lorem ipsum dolor", true, "Component1:\n  Control: Component\n  Description: lorem ipsum dolor\n  AccessAppScope: true\n")]
    [DataRow(ComponentType.Canvas, "", true, "Component1:\n  Control: Component\n  AccessAppScope: true\n")]
    [DataRow(ComponentType.Command, "", true, "Component1:\n  Control: Component\n  Type: Command\n  AccessAppScope: true\n")]
    public void Serialize_ShouldCreateValidYamlForComponentDefinitions(ComponentType componentType, string description, bool accessAppScope, string expectedYaml)
    {
        var component = (ComponentDefinition)ControlFactory.Create("Component1", "Component");
        component.Should().NotBeNull();
        component.Type = componentType;
        component.Description = description;
        component.AccessAppScope = accessAppScope;

        var sut = CreateSerializer();

        var serializedComponent = sut.SerializeControl(component).NormalizeNewlines();

        serializedComponent.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-component-instance.pa.yaml", true)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-component-instance.pa.yaml", false)]
    public void Serialize_ShouldCreateValidYamlForComponentInstance(string expectedPath, bool isControlIdentifiers)
    {
        var graph = ControlFactory.CreateScreen("Hello",
                        properties: new(),
                        children: new Control[]
                        {
                            new ComponentInstance()
                            {
                                Name = "This is custom component",
                                ComponentName = "ComponentDefinition_1",
                                ComponentLibraryUniqueName = "MyComponentLibrary",
                                Variant = string.Empty,
                                Template = new Microsoft.PowerPlatform.PowerApps.Persistence.Templates.ControlTemplate("http://localhost/Component"),
                                Properties = new()
                                {
                                    new ControlProperty("X", "15"),
                                    new ControlProperty("Y", "55"),
                                }
                            }
                        });
        var sut = CreateSerializer(isControlIdentifiers: isControlIdentifiers);

        var serializedGraph = sut.SerializeControl(graph).NormalizeNewlines();

        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        serializedGraph.Should().Be(expectedYaml);
    }
}
