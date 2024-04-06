// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializationFactory
{
    IYamlSerializer CreateSerializer(YamlSerializationOptions? options = null);

    IYamlDeserializer CreateDeserializer(YamlSerializationOptions? options = null);
}
