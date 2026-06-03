// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public interface IValidatorFactory
{
    IValidator CreateValidator();
}
