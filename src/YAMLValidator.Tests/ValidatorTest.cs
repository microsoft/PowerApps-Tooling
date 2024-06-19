// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Microsoft.PowerPlatform.PowerApps.Persistence;

namespace YamlValidatorTests;

[TestClass]
public class ValidatorTest
{
    private const string _validPath = @".\_TestData\ValidYaml";
    private const string _invalidPath = @".\_TestData\InvalidYaml";
    private const string _schemaPath = @"..\YAMLValidator\schema\pa.yaml-schema.json";

    private readonly JsonSchema _schema;
    private readonly Validator _yamlValidator;

    public ValidatorTest()
    {
        var schemaFileLoader = new SchemaLoader();
        _schema = schemaFileLoader.Load(_schemaPath);
        var resultVerbosity = new VerbosityData(YamlValidatorConstants.verbose);
        _yamlValidator = new Validator(resultVerbosity.EvalOptions, resultVerbosity.JsonOutputOptions);
    }

    // to do make validator object computation into a function -> clean up code

    // invalid schemas where yaml or schema is missing
    // invalid yaml tests

    // This yaml fails the schema oneOf clause, it is simply a key with no colon
    [TestMethod]
    public void TestScreenNameNoColon()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ScreenWithNameNoValue.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }

    // This yaml fails the schema oneOf clause, it is simply a key with no value
    [TestMethod]
    public void TestScreenWithNameNoValue()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ScreenWithNameNoValue.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }

    // a control should have the property indicating which control type it is, this doesn't
    [TestMethod]
    public void TestScreenWithoutControlProperty()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ScreenWithNameNoValue.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);

    }


    // Control should be of type object, this isn't
    [TestMethod]
    public void TestWrongControlDefinition()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\WrongControlDefinition.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }

    // a control with an invalid property (not in the schema)
    [TestMethod]
    public void ControlWithInvalidProperty()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ControlWithInvalidProperty.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }

    [TestMethod]
    public void ControlWithAdditionalProperty()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ControlObjectWithAdditionalProperty.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsFalse(result.SchemaValid);
    }


    // valid yaml
    // note powerapps studio wont allow you to have a screen without a name
    // This is a runtime error, but not a syntax error

    [TestMethod]
    public void TestEmptyYaml()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\Empty.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsTrue(result.SchemaValid);
    }

    // syntactically correct -> an app which matches the regex for a screen and has a control
    [TestMethod]
    public void TestNameLessObjectWithControl()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\NamelessObjectWithControl.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsTrue(result.SchemaValid);
    }

    // a working screen on powerapps studio
    [TestMethod]
    public void TestStudioMadeApp()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\ValidScreen1.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsTrue(result.SchemaValid);
    }

    // a simple app without any recursive definitions
    [TestMethod]
    public void TestSimpleNoRecursiveDefinition()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\SimpleNoRecursiveDefinition.yaml");
        var result = _yamlValidator.Validate(_schema, rawYaml);
        Assert.IsTrue(result.SchemaValid);

    }

}
