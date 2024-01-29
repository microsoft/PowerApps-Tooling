// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public static class YamlUtils
{
    #region Constants

    public const string YamlFileExtension = ".yaml";
    public const string YmlFileExtension = ".yml";
    public const string YamlFxFileExtension = ".fx.yaml";

    #endregion

    /// <summary>
    /// Checks if the given path is a yaml file.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool IsYamlFile(string path)
    {
        return
            Path.GetExtension(path).Equals(YamlFileExtension, StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(path).Equals(YmlFileExtension, StringComparison.OrdinalIgnoreCase);
    }
}

