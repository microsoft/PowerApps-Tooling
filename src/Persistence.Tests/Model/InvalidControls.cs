// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Model;

[TestClass]
public class InvalidControls
{
    [TestMethod]
    [DataRow("")]
    [DataRow("           ")]
    public void Constructor_InvalidControlName_Throws(string controlName)
    {
        // Act
        Action act = () => new Screen(controlName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
