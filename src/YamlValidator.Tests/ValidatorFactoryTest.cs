// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

namespace Persistence.YamlValidator.Tests;

[TestClass]
public class ValidatorFactoryTest : TestBase
{
    [TestMethod]
    public void GetValidatorTest()
    {
        var factory = new ValidatorFactory();
        var validator = factory.CreateValidator();

        Assert.IsNotNull(validator);
        Assert.IsInstanceOfType(validator, typeof(IValidator));
    }
}
