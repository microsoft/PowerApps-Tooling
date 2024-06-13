// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class YamlLoader
{
    public IReadOnlyDictionary<string, string> _yamlData { get; }
    // move this and all occurences of it to a utility class and make it static
    //private const string _yamlFileExtension = ".yaml";
    //private const string _fileTypeName = "file";
    // private const string FolderTypeName = "folder";

    public YamlLoader(string filePath, string pathType)
    {
        var loadedYaml = LoadFilePathData(filePath, pathType);
        _yamlData = new ReadOnlyDictionary<string, string>(loadedYaml);
    }
    private static Dictionary<string, string> LoadFilePathData(string filePath, string pathType)
    {
        var deserializedYaml = new Dictionary<string, string>();
        if (pathType == YamlValidatorConstants.FileTypeName)
        {
            var fileName = Path.GetFileName(filePath);
            var yamlText = YamlValidatorUtility.ReadFileData(filePath);
            deserializedYaml.Add(fileName, yamlText);
            return deserializedYaml;
        }

        // to do: address edge case of .yml files
        var files = Directory.GetFiles(filePath, $"*{YamlValidatorConstants.YamlFileExtension}");
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var yamlText = YamlValidatorUtility.ReadFileData(file);
            deserializedYaml.Add(fileName, yamlText);
        }
        return deserializedYaml;
    }

}
