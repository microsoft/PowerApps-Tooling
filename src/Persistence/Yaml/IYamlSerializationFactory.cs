// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializationFactory
{
    ISerializer CreateSerializer(bool? isTextFirst = null);

    IDeserializer CreateDeserializer(bool? isTextFirst = null);
}
