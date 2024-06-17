// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class YamlLoader
{

    public IReadOnlyDictionary<string, string> Load(string filePath, string pathType)
    {
        var deserializedYaml = new Dictionary<string, string>();
        if (pathType == YamlValidatorConstants.FileTypeName)
        {
            var fileName = Path.GetFileName(filePath);
            var yamlText = YamlValidatorUtility.ReadFileData(filePath);
            deserializedYaml.Add(fileName, yamlText);
            return new ReadOnlyDictionary<string, string>(deserializedYaml);
        }

        // to do: address edge case of .yml files
        var files = Directory.GetFiles(filePath, $"*{YamlValidatorConstants.YamlFileExtension}");
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var yamlText = YamlValidatorUtility.ReadFileData(file);
            deserializedYaml.Add(fileName, yamlText);
        }

        return new ReadOnlyDictionary<string, string>(deserializedYaml);
    }

}
