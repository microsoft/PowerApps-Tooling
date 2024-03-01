// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Persistence.Tests.Extensions;

#pragma warning disable CS8600
#pragma warning disable CS8604

[TestClass]
public class StringExtensionsTest
{
    [TestMethod]
    public void FirstCharToUpper_NullInput_ReturnsNull()
    {
        string input = null;
        Assert.IsNull(input.FirstCharToUpper());
    }

    [TestMethod]
    public void FirstCharToUpper_EmptyInput_ReturnsEmpty()
    {
        var input = string.Empty;
        Assert.AreEqual(string.Empty, input.FirstCharToUpper());
    }

    [TestMethod]
    public void FirstCharToUpper_LowerCaseInput_ReturnsUpperCase()
    {
        var input = "hello";
        Assert.AreEqual("Hello", input.FirstCharToUpper());
    }

    [TestMethod]
    public void FirstCharToUpper_UpperCaseInput_ReturnsUpperCase()
    {
        var input = "Hello";
        Assert.AreEqual("Hello", input.FirstCharToUpper());
    }
}

#pragma warning restore CS8600
#pragma warning restore CS8604
