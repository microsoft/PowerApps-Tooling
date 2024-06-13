// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence;

namespace YamlValidatorTests;

[TestClass]
public class ValidatorTest
{


    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TestSchemaAndJsonEmpty()
    {
        var validator = new Validator();
        validator.Validate();

    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TestSchemaEmpty()
    {
        var validator = new Validator();
        validator.Validate();

    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TestSchemaYamlEmpty()
    {
        var validator = new Validator();
        validator.Validate();

    }





}
