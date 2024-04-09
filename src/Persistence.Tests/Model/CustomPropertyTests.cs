// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Model;

[TestClass]
public class CustomPropertyTests
{
    [TestMethod]
    [DataRow(CustomProperty.PropertyType.Data, PropertyCategory.Data)]
    [DataRow(CustomProperty.PropertyType.Event, PropertyCategory.Behavior)]
    [DataRow(CustomProperty.PropertyType.Function, PropertyCategory.Data)]
    [DataRow(CustomProperty.PropertyType.Action, PropertyCategory.Behavior)]
    public void Category_shouldBeValidBasedOnType(CustomProperty.PropertyType propertyType, PropertyCategory expectedCategory)
    {
        var sut = new CustomProperty
        {
            Name = "Test",
            Type = propertyType
        };

        // Assert
        sut.Category.Should().Be(expectedCategory);
    }
}
