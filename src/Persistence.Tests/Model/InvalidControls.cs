// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Persistence.Tests.Model;

[TestClass]
public class InvalidControls : TestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("           ")]
    public void Constructor_InvalidControlName_Throws(string controlName)
    {
        // Act
        Action act = () => ControlFactory.CreateScreen(controlName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
