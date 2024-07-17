// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public static class Constants
{
<<<<<<< HEAD
    public const string YamlFileExtension = ".pa.yaml";
    public const string JsonFileExtension = ".json";

    public const string notYamlError = "File is not YAML";
    public const string emptyYamlError = "Empty YAML file";

    public const string subNamespace = "YamlValidator";
=======
    public const string FileTypeName = "file";
    public const string FolderTypeName = "folder";
    public const string YamlFileExtension = ".pa.yaml";
    public const string JsonFileExtension = ".json";

    public const string Verbose = "verbose";

    // runtime constants
    // default schema path
    public static readonly string DefaultSchemaPath = Path.Combine(".", "schema", "pa.yaml-schema.json");
>>>>>>> master
}
