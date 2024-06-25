// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

namespace Persistence.Tests.YamlValidator;

[TestClass]
public class ValidatorTest
{
    private const string _validPath = @".\_TestData\ValidYaml";
    private const string _invalidPath = @".\_TestData\InvalidYaml";
    private const string _schemaPath = @"..\YamlValidator\schema\pa.yaml-schema.json";

    private readonly JsonSchema _schema;
    private readonly Validator _yamlValidator;

    public ValidatorTest()
    {
        var schemaFileLoader = new SchemaLoader();
        _schema = schemaFileLoader.Load(_schemaPath);
        var resultVerbosity = new VerbosityData(Constants.Verbose);
        _yamlValidator = new Validator(resultVerbosity.EvalOptions, resultVerbosity.JsonOutputOptions);
    }

    [TestMethod]
    [DataRow($@"{_invalidPath}\ScreenWithNameNoColon.yaml", false)]
    [DataRow($@"{_invalidPath}\ScreenWithNameNoValue.yaml", false)]
    [DataRow($@"{_invalidPath}\ScreenWithoutControlProperty.yaml", false)]
    [DataRow($@"{_invalidPath}\WrongControlDefinition.yaml", false)]
    [DataRow($@"{_invalidPath}\ControlWithInvalidProperty.yaml", false)]
    [DataRow($@"{_invalidPath}\EmptyArray.yaml", false)]
    [DataRow($@"{_invalidPath}\Empty.yaml", false)]
    [DataRow($@"{_invalidPath}\NamelessObjectNoControl.yaml", false)]
    [DataRow($@"{_validPath}\NamelessObjectWithControl.yaml", true)]
    [DataRow($@"{_validPath}\ValidScreen1.yaml", true)]
    [DataRow($@"{_validPath}\SimpleNoRecursiveDefinition.yaml", true)]

    public void TestValidation(string filepath, bool expectedResult)
    {
        var rawYaml = Utility.ReadFileData($@"{filepath}");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsTrue(result.SchemaValid == expectedResult);
    }
}
