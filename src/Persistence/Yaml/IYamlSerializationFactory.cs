// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializationFactory
{
    IYamlSerializer CreateSerializer(bool? isTextFirst = null);

    IYamlDeserializer CreateDeserializer(bool? isTextFirst = null);
}
