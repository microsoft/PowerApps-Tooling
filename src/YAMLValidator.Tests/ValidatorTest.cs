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
        _yamlValidator.Yaml = YamlValidatorUtility.ReadFileData($@"{_validPath}\Simple.yaml");
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


    // valid yaml
    // note powerapps studio wont allow you to have a screen without a name
    // we may want to modify this as an edge case to return false

    [TestMethod]
    public void TestEmptyYaml()
    {
        var rawYaml = YamlValidatorUtility.ReadFileData($@"{_invalidPath}\Empty.yaml");
        SchemaLoader _schemaLoader = new(_schemaPath);
        _yamlValidator.Schema = _schemaLoader.Schema;
        _yamlValidator.Yaml = rawYaml;
        var result = _yamlValidator.Validate();
        Assert.IsTrue(result);
    }





}
