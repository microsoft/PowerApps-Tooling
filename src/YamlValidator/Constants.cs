// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public static class Constants
{
    public const string FileTypeName = "file";
    public const string FolderTypeName = "folder";
    public const string YamlFileExtension = ".pa.yaml";
    public const string JsonFileExtension = ".json";

    public const string Verbose = "verbose";

    // runtime constants
    // default schema path
    public static readonly string[] DefaultSchemaPath = [".", "schema", "pa.yaml-schema.json"];
}
