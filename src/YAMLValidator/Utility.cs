// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public class Utility
{
    public static string ReadFileData(string filePath)
    {
        var yamlData = File.ReadAllText(filePath);
        return yamlData;
    }

    public static YamlStream MakeYamlStream(string yamlString)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlString));
        return stream;
    }
}
