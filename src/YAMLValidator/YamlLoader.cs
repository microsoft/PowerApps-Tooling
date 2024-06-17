// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class YamlLoader
{
    public IReadOnlyDictionary<string, string> YamlData { get; }

    public YamlLoader(string filePath, string pathType)
    {
        var loadedYaml = LoadFilePathData(filePath, pathType);
        YamlData = new ReadOnlyDictionary<string, string>(loadedYaml);
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
