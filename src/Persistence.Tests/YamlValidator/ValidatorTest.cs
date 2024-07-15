// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

namespace Persistence.Tests.YamlValidator;

[TestClass]
public class ValidatorTest
{

    private static readonly string _validPath = Path.Combine(".", "_TestData", "ValidatorTests", "ValidYaml") +
        Path.DirectorySeparatorChar;

    private static readonly string _invalidPath = Path.Combine(".", "_TestData", "ValidatorTests", "InvalidYaml") +
        Path.DirectorySeparatorChar;

    private readonly Validator _yamlValidator;

    public ValidatorTest()
    {
        var validatorFactory = new ValidatorFactory();
        _yamlValidator = validatorFactory.GetValidator();
    }

    [TestMethod]
    [DataRow("NamelessObjectWithControl.yaml")]
    [DataRow("ValidScreen1.yaml")]
    [DataRow("SimpleNoRecursiveDefinition.yaml")]

    public void TestValidationValidYaml(string filename)
    {
        var rawYaml = File.ReadAllText($@"{_validPath}{filename}");
        var result = _yamlValidator.Validate(rawYaml);
        Assert.IsTrue(result.SchemaValid);
    }

    [TestMethod]
    [DataRow("ScreenWithNameNoColon.yaml")]
    [DataRow("ScreenWithNameNoValue.yaml")]
    [DataRow("ScreenWithoutControlProperty.yaml")]
    [DataRow("WrongControlDefinition.yaml")]
    [DataRow("ControlWithInvalidProperty.yaml")]
    [DataRow("EmptyArray.yaml")]
    [DataRow("Empty.yaml")]
    [DataRow("NamelessObjectNoControl.yaml")]
    [DataRow("NotYaml.yaml")]
    public void TestValidationInvalidYaml(string filename)
    {
        var rawYaml = File.ReadAllText($@"{_invalidPath}{filename}");
        var result = _yamlValidator.Validate(rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }
}
