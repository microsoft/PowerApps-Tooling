// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
//using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Test.AppWriter;

internal class Program
{
    private static ServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    private static App GetExampleApp(ServiceProvider provider, string appname, int numscreens = 1)
    {
        var app = provider.GetRequiredService<IControlFactory>().CreateApp(appname);
        for (var i = 0; i < numscreens; i++)
        {
            var graph = provider.GetRequiredService<IControlFactory>().CreateScreen("Screen" + i.ToString(),
                properties: new Dictionary<string, ControlPropertyValue>()
                {
                    { "Text", new() { Value = "I am a screen" }  },
                },
                controls: new Control[]
                {
                    provider.GetRequiredService<IControlFactory>().Create("Label1", template: "text",
                        properties: new Dictionary<string, ControlPropertyValue>()
                        {
                            { "Text", new() { Value = "lorem ipsum" }  },
                        }
                    ),
                    provider.GetRequiredService<IControlFactory>().Create("Button1", template: "button",
                        properties: new Dictionary<string, ControlPropertyValue>()
                        {
                            { "Text", new() { Value = "click me" }  },
                            { "X", new() { Value = "100" } },
                            { "Y", new() { Value = "200" } }
                        }
                    )
                }
            );
            app.Screens.Add(graph);
        }
        return app;
    }

    private static void Main(string[] args)
    {
        var provider = ConfigureServiceProvider();
        // var msappArchiveFactory = provider.GetRequiredService<IMsappArchiveFactory>();
        // var controlFactory = provider.GetRequiredService<IControlFactory>();
        // var templateStore = provider.GetRequiredService<IControlTemplateStore>();

        // Console.WriteLine(Directory.GetCurrentDirectory());
        // GetCommandLineArgs()
        var fullPathToMsApp = args.Length > 0 ? args[0] : null;
        var appname = "appname";

        if (fullPathToMsApp == null)
        {
            Console.WriteLine("No args provided, using default file path");
            fullPathToMsApp = Directory.GetCurrentDirectory() + "\\appname.msapp";
            Console.WriteLine(fullPathToMsApp);
        }

        if (File.Exists(fullPathToMsApp)) // Overwrite
        {
            Console.WriteLine("Warning: File already exists;  Overwrite? (y / n)");
            var input = Console.ReadLine();
            if (input?.ToLower()[0] == 'y') File.Delete(fullPathToMsApp);
        }

        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        msapp.App = GetExampleApp(provider, appname, 2);
        msapp.Save();
    }
}
