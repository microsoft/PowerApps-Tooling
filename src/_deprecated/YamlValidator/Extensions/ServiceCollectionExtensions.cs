// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// registers the MSAPP persistence services
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    public static void AddPowerAppsPersistenceYamlValidator(this IServiceCollection services)
    {
        services.AddSingleton<IValidatorFactory, ValidatorFactory>();
    }
}


