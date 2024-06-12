// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class YamlValidatorUtility
{
    public static string ReadFileData(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var yamlData = reader.ReadToEnd();
            return yamlData;
        }
        catch (IOException)
        {
            Console.WriteLine("The given file couldn't be read");
            throw;
        }
    }

    public static YamlStream MakeYamlStream(string yamlString)
    {
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yamlString));
            return stream;
        }
        catch (YamlException)
        {
            Console.WriteLine("The given file isn't valid YAML");
            throw;
        }
    }
}
