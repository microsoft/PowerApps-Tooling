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
        var graph = ControlFactory.Create("Screen1", "Screen",
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
        var graph = ControlFactory.Create("CustomControl1", template: "http://localhost/#customcontrol",
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
        var graph = ControlFactory.Create("BuiltIn Control1", template: templateName,
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
        var graph = ControlFactory.Create("built in", "Button", variant: expectedVariant,
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
        var serializer = CreateSerializer(isControlIdentifiers);

        var sut = serializer.Serialize(graph).NormalizeNewlines();
        var expectedYaml = File.ReadAllText(GetTestFilePath(expectedPath, isControlIdentifiers)).NormalizeNewlines();
        sut.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DynamicData(nameof(ComponentCustomProperties_Data), typeof(TestBase))]
    public void Serialize_ShouldCreateValidYamlForComponentCustomProperties(CustomProperty[] customProperties, string expectedYamlFile, bool isControlIdentifiers)
    {
        var component = ControlFactory.Create("Component1", "Component") as Component;
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
    [DataRow("lorem ipsum dolor", false, "Control: Component\nName: Component1\nDescription: lorem ipsum dolor\n")]
    [DataRow("lorem ipsum dolor", true, "Control: Component\nName: Component1\nDescription: lorem ipsum dolor\nAccessAppScope: true\n")]
    [DataRow("", true, "Control: Component\nName: Component1\nAccessAppScope: true\n")]
    public void Serialize_ShouldCreateValidYamlForComponent(string description, bool accessAppScope, string expectedYaml)
    {
        var component = (Component)ControlFactory.Create("Component1", "Component");
        component.Should().NotBeNull();
        component.Description = description;
        component.AccessAppScope = accessAppScope;

        var sut = CreateSerializer();

        var serializedComponent = sut.SerializeControl(component).NormalizeNewlines();

        serializedComponent.Should().Be(expectedYaml);
    }
}
