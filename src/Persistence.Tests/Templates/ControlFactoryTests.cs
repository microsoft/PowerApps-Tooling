// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Persistence.Tests.Templates;

[TestClass]
public class ControlFactoryTests : TestBase
{
    [TestMethod]
    public void CreateFromTemplate_ShouldSetControlPropertiesWhenAvailable()
    {
        var expectedProps = new ControlPropertiesCollection
        {
            { "Property1", "Value1" },
            { "Property2", "Value2" }
        };

        var template = ControlTemplateStore.GetByName("Screen");

        var sut = new ControlFactory(ControlTemplateStore);

        var result = sut.Create("Screen", template, properties: expectedProps);
        result.Should().NotBeNull();
        result.Properties.Should().NotBeNull()
           .And.BeEquivalentTo(expectedProps);
    }

    [TestMethod]
    public void CreateFromTemplate_ShouldNotSetControlPropertiesWhenNotAvailable()
    {
        var template = ControlTemplateStore.GetByName("Screen");

        var sut = new ControlFactory(ControlTemplateStore);

        var result = sut.Create("Screen", template, properties: null);
        result.Should().NotBeNull();
        result.Properties.Should().NotBeNull()
            .And.BeEmpty();
    }

    [TestMethod]
    public void CreateFromTemplateName_Should_CreateFirstClassTypes()
    {
        var sut = new ControlFactory(ControlTemplateStore);

        var result = sut.Create("Screen1", "Screen", properties: null);
        result.Should().NotBeNull().And.BeOfType<Screen>();
        result.Properties.Should().NotBeNull().And.BeEmpty();
        result.Children.Should().BeNull();
    }

    [TestMethod]
    [DataRow("Component", "MyComponent1", "http://microsoft.com/appmagic/Component", ComponentType.Canvas)]
    [DataRow("CommandComponent", "MyComponent1", "http://microsoft.com/appmagic/CommandComponent", ComponentType.Command)]
    [DataRow("DataComponent", "MyComponent1", "http://microsoft.com/appmagic/DataComponent", ComponentType.Data)]
    [DataRow("FunctionComponent", "MyComponent1", "http://microsoft.com/appmagic/FunctionComponent", ComponentType.Function)]
    public void CreateComponent_ShouldCreateValidInstance(string componentType, string componentName, string expectedTemplateId, ComponentType expectedType)
    {
        var sut = new ControlFactory(ControlTemplateStore);

        var result = sut.Create(componentName, componentType);
        result.Should().NotBeNull().And.BeAssignableTo<ComponentDefinition>();

        var componendDef = (ComponentDefinition)result;
        componendDef.Name.Should().Be(componentName);
        componendDef.Template.Should().NotBeNull();
        componendDef.Template.Id.Should().Be(expectedTemplateId);
        componendDef.Template.Name.Should().Be(componentName);
        componendDef.Properties.Should().NotBeNull().And.BeEmpty();
        componendDef.Children.Should().BeNull();
        componendDef.Type.Should().Be(expectedType);
    }
}
