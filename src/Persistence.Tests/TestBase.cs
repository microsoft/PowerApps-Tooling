// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

namespace Persistence.Tests;

public abstract class TestBase : VSTestBase
{
    public static IServiceProvider ServiceProvider { get; set; }

    public IControlTemplateStore ControlTemplateStore { get; private set; }

    public IMsappArchiveFactory MsappArchiveFactory { get; private set; }

    public IControlFactory ControlFactory { get; private set; }

    public IValidatorFactory ValidatorFactory { get; private set; }

    static TestBase()
    {
        ServiceProvider = BuildServiceProvider();
    }

    public TestBase()
    {
        // Request commonly used services
        ControlTemplateStore = ServiceProvider.GetRequiredService<IControlTemplateStore>();
        MsappArchiveFactory = ServiceProvider.GetRequiredService<IMsappArchiveFactory>();
        ControlFactory = ServiceProvider.GetRequiredService<IControlFactory>();
        ValidatorFactory = ServiceProvider.GetRequiredService<IValidatorFactory>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = ConfigureServices(serviceCollection);

        return serviceProvider;
    }

    private static ServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddPowerAppsPersistence(useDefaultTemplates: true);
        services.AddPowerAppsPersistenceYamlValidator();

        return services.BuildServiceProvider();
    }

    public static IYamlDeserializer CreateDeserializer(bool isControlIdentifiers = false, bool isTextFirst = false)
    {
        return ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer
        (
            new YamlSerializationOptions
            {
                IsTextFirst = isTextFirst,
                IsControlIdentifiers = isControlIdentifiers
            }
        );
    }

    public static IYamlSerializer CreateSerializer(bool isControlIdentifiers = false, bool isTextFirst = false)
    {
        return ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer
        (
            new YamlSerializationOptions
            {
                IsTextFirst = isTextFirst,
                IsControlIdentifiers = isControlIdentifiers
            }
        );
    }

    public static string GetTestFilePath(string path, bool isControlIdentifiers = false)
    {
        return string.Format(CultureInfo.InvariantCulture, path, isControlIdentifiers ? "-CI" : string.Empty);
    }

    public static IEnumerable<object[]> ComponentCustomProperties_Data => new List<object[]>()
    {
        new object[]
        {
            new CustomProperty[] {
                new () { Name = "MyTextProp1", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Data
                }
            },
            @"_TestData/ValidYaml{0}/ComponentDefinitions/CustomProperty1.pa.yaml", false
        },
        new object[]
        {
            new CustomProperty[] {
                new () { Name = "MyTextProp1", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Data
                }
            },
            @"_TestData/ValidYaml{0}/ComponentDefinitions/CustomProperty1.pa.yaml", true
        },
        new object[]
        {
            new CustomProperty[] {
                new () { Name = "MyFuncProp1", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Function,
                    Parameters = new[] {
                        new CustomPropertyParameter(){
                            Name = "param1",
                            DataType = "String",
                            IsRequired = true,
                        }
                    },
                }
            },
            @"_TestData/ValidYaml{0}/ComponentDefinitions/CustomProperty2.pa.yaml", false
        },
        new object[]
        {
            new CustomProperty[] {
                new () { Name = "MyFuncProp1", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Function,
                    Parameters = new[] {
                        new CustomPropertyParameter(){
                            Name = "param1",
                            DataType = "String",
                            IsRequired = true,
                        }
                    },
                }
            },
            @"_TestData/ValidYaml{0}/ComponentDefinitions/CustomProperty2.pa.yaml", true
        },
        new object[]
        {
            new CustomProperty[] {
                new () { Name = "MyTextProp1", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Data
                },
                new () { Name = "MyTextProp2", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Data
                }
            },
            @"_TestData/ValidYaml{0}/ComponentDefinitions/with-two-properties.pa.yaml", false
        },
        new object[]
        {
            new CustomProperty[] {
                new () { Name = "MyTextProp1", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Data
                },
                new () { Name = "MyTextProp2", DataType = "String", Default = "lorem",
                    Direction = CustomProperty.PropertyDirection.Input, Type = CustomProperty.PropertyType.Data
                }
            },
            @"_TestData/ValidYaml{0}/ComponentDefinitions/with-two-properties.pa.yaml", true
        },

    };

}
