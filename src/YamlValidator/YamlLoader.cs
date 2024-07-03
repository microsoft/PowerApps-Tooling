// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public class YamlLoader
{
    public IReadOnlyDictionary<string, string> Load(string filePath, string pathType)
    {
        var deserializedYaml = new Dictionary<string, string>();

        if (pathType == Constants.FileTypeName)
        {
            var fileName = Path.GetFileName(filePath);
            var yamlText = Utility.ReadFileData(filePath);
            deserializedYaml.Add(fileName, yamlText);
        }
        else if (pathType == Constants.FolderTypeName)
        {
            // TODO: Determine if argument flag should be required to specify recursive folder search
            try
            {
                var yamlFiles = Directory.EnumerateFiles(filePath, "*" + Constants.YamlFileExtension, SearchOption.AllDirectories);
                foreach (var filename in yamlFiles)
                {
                    var fileName = Path.GetFullPath(filename);
                    var yamlText = Utility.ReadFileData(filename);
                    deserializedYaml.Add(fileName, yamlText);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Unauthorized access exception: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO exception: {ex.Message}");
            }
        }
        else
        {
            throw new ArgumentException("Invalid path type");
        }

        return new ReadOnlyDictionary<string, string>(deserializedYaml);
    }
}
