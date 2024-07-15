// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

namespace Persistence.Tests.YamlValidator;

[TestClass]
public class ValidatorFactoryTest
{
    [TestMethod]
    public void GetValidatorTest()
    {
        var factory = new ValidatorFactory();
        var validator = factory.GetValidator();

        Assert.IsNotNull(validator);
        Assert.IsInstanceOfType(validator, typeof(Validator));
    }
}
