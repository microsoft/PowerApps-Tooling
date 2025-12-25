// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Persistence.Tests;

public abstract class TestBase : VSTestBase
{
    public static IServiceProvider ServiceProvider { get; set; }

    public IMsappArchiveFactory MsappArchiveFactory { get; private set; }

    static TestBase()
    {
        ServiceProvider = BuildServiceProvider();
    }

    public TestBase()
    {
        // Request commonly used services
        MsappArchiveFactory = ServiceProvider.GetRequiredService<IMsappArchiveFactory>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddMsappArchiveFactory();
    }

    public static string GetTestFilePath(string path, bool isControlIdentifiers = false)
    {
        return string.Format(CultureInfo.InvariantCulture, path, isControlIdentifiers ? "-CI" : string.Empty);
    }
}
