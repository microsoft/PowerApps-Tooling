// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence;

namespace YamlValidatorTests;

[TestClass]
public class ValidatorTest
{
    private const string _validPath = @".\_TestData\ValidYaml";
    private const string _invalidPath = @".\_TestData\InvalidYaml";
    private const string _schemaPath = @"..\YAMLValidator\schema\pa.yaml-schema.json";

    private readonly Validator _yamlValidator = new();

    // to do make validator object computation into a function -> clean up code

    // invalid schemas where yaml or schema is missing
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TestSchemaAndJsonEmpty()
    {
        _yamlValidator.Validate();

    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TestSchemaEmpty()
    {
        _yamlValidator.Yaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\SimpleNoRecursiveDefinition.yaml");
        _yamlValidator.Validate();

    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void TestYamlEmpty()
    {

        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Validate();

    }
    // invalid yaml tests

    // This yaml fails the schema oneOf clause, it is simply a key with no colon
    [TestMethod]
    public void TestScreenNameNoColon()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ScreenWithNameNoColon.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsFalse(result);

    }

    // This yaml fails the schema oneOf clause, it is simply a key with no value
    [TestMethod]
    public void TestScreenWithNameNoValue()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ScreenWithNameNoValue.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsFalse(result);

    }

    // a control should have the property indicating which control type it is, this doesn't
    [TestMethod]
    public void TestScreenWithoutControlProperty()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ScreenWithoutControlProperty.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsFalse(result);

    }

    // Control should be of type object, this isn't
    [TestMethod]
    public void TestWrongControlDefinition()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\WrongControlDefinition.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsFalse(result);
    }

    // a control with an invalid property (not in the schema)
    [TestMethod]
    public void ControlWithInvalidProperty()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ControlWithInvalidProperty.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ControlWithAdditionalProperty()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\ControlObjectWithAdditionalProperty.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsFalse(result);
    }


    // valid yaml
    // note powerapps studio wont allow you to have a screen without a name
    // This is a runtime error, but not a syntax error

    [TestMethod]
    public void TestEmptyYaml()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\Empty.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsTrue(result);
    }

    // syntactically correct -> an app which matches the regex for a screen and has a control
    [TestMethod]
    public void TestNameLessObjectWithControl()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\NamelessObjectWithControl.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsTrue(result);
    }

    // a working screen on powerapps studio
    [TestMethod]
    public void TestStudioMadeApp()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\ValidScreen1.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsTrue(result);
    }

    // a simple app without any recursive definitions
    [TestMethod]
    public void TestSimpleNoRecursiveDefinition()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\SimpleNoRecursiveDefinition.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsTrue(result);

    }

}
