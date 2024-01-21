// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;

namespace PAModelTests;

[TestClass]
public class DefaultValuesTransformTests
{
    public static readonly List<string> dynamicPropertiesList = new() { "LayoutX", "LayoutY", "LayoutWidth", "LayoutHeight" };

    // Testing the DefaultValuesTransform:beforeWrite() behavior on control node with null dynamic properties
    // Specifically cases where property null but propertynames not null
    [TestMethod]
    public void TestCaseWithNullDynamicProperties()
    {
        // creating a BlockNode with a single property
        var newNode = new BlockNode()
        {
            Name = new TypedNameNode()
            {
                Identifier = "Canvas1",
                Kind = new TypeNode() { TypeName = "fluidGrid", OptionalVariant = "fluidGridWithBlankCard" }
            },
            Properties = new List<PropertyNode>() { new PropertyNode { Identifier = "SomeProp", Expression = new ExpressionNode() { Expression = "Expr" } } }
        };

        var defaultValTransform = new DefaultValuesTransform(getTemplateStore(), getTheme(), getEditorStateStore());

        defaultValTransform.BeforeWrite(newNode, false);

        var nodeProperties = newNode.Properties;
        foreach (var property in nodeProperties)
        {
            // check if dynamic property with null values are added, if so fail the test
            if (dynamicPropertiesList.Contains(property.Identifier))
            {
                Assert.Fail($"Dynamic property {property.Identifier} with null value added.");
            }
        }

        // nodeProperties after BeforeWrite() contains only the SomeProp and none of the null dynamic properties
        Assert.IsTrue(nodeProperties.Count == 1);
    }

    private static EditorStateStore getEditorStateStore()
    {
        // creating dynamic properties with Property value null, but with PropertyName
        var dynPropStates = new List<DynamicPropertyState>();
        dynPropStates.AddRange(new List<DynamicPropertyState> {
            new DynamicPropertyState() { PropertyName = "LayoutX" },
            new DynamicPropertyState() { PropertyName = "LayoutY" },
            new DynamicPropertyState() { PropertyName = "LayoutWidth" },
            new DynamicPropertyState() { PropertyName = "LayoutHeight" },
        });

        var editorStateStore = new EditorStateStore();
        var someProperty = new PropertyState() { PropertyName = "SomeProperty" };
        editorStateStore.TryAddControl(new ControlState()
        {
            Name = "Canvas1",
            TopParentName = "Screen1",
            DynamicProperties = dynPropStates,
            HasDynamicProperties = true,
            Properties = new List<PropertyState> { someProperty },
            StyleName = "fluidGrid"
        });

        return editorStateStore;
    }

    private static Theme getTheme()
    {
        var CustomTheme = new CustomThemeJson() { name = "SomeCustomTheme" };
        var themeJson = new ThemesJson() { CurrentTheme = "SomeTheme", CustomThemes = new[] { CustomTheme } };

        return new Theme(themeJson);
    }

    // To Load the fluidGrid template defaults
    private static Dictionary<string, ControlTemplate> getTemplateStore()
    {
        var parsedTemplates = new Dictionary<string, ControlTemplate>();
        var fluidGridTemplatePath = Path.Combine(Environment.CurrentDirectory, "Templates", "fluidGrid_2.2.0.xml");
        File.Exists(fluidGridTemplatePath).Should().BeTrue();

        using var fluidGridTemplateStream = File.OpenRead(fluidGridTemplatePath);
        using var fluidGridTemplateReader = new StreamReader(fluidGridTemplateStream);
        var templateStore = new TemplateStore();
        var fluidGridTemplateContents = fluidGridTemplateReader.ReadToEnd();
        ControlTemplateParser.TryParseTemplate(templateStore, fluidGridTemplateContents, AppType.DesktopOrTablet, parsedTemplates, out var topTemplate, out var name);

        return parsedTemplates;
    }
}
