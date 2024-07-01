// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

namespace Persistence.Tests.YamlValidator;

[TestClass]
public class ValidatorTest
{

    private static readonly string _validPath = Path.Combine(".", "_TestData", "ValidYaml") +
        Path.DirectorySeparatorChar;

    private static readonly string _invalidPath = Path.Combine(".", "_TestData", "InvalidYaml") +
        Path.DirectorySeparatorChar;

    private readonly JsonSchema _schema;
    private readonly Validator _yamlValidator;

    public ValidatorTest()
    {
        var schemaFileLoader = new SchemaLoader();
        _schema = schemaFileLoader.Load(Constants.DefaultSchemaPath);
        var resultVerbosity = new VerbosityData(Constants.Verbose);
        _yamlValidator = new Validator(resultVerbosity.EvalOptions, resultVerbosity.JsonOutputOptions);
    }

    [TestMethod]
    [DataRow("NamelessObjectWithControl.yaml")]
    [DataRow("ValidScreen1.yaml")]
    [DataRow("SimpleNoRecursiveDefinition.yaml")]

    public void TestValidationValidYaml(string filename)
    {
        var rawYaml = Utility.ReadFileData($@"{_validPath}{filename}");
        var result = _yamlValidator.Validate(_schema, rawYaml);
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
    public void TestValidationInvalidYaml(string filename)
    {
        var rawYaml = Utility.ReadFileData($@"{_invalidPath}{filename}");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }
}
