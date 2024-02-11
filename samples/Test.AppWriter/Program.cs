// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Test.AppWriter;

internal class Program
{
    // Configures default services for generating the MSApp representation
    private static IServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    // This produces a simple example app including the specified number of screens and a few generic controls
    // This is intended for testing purposes only
    private static App GetExampleApp(IServiceProvider provider, string appName)
    {
        var controlFactory = provider.GetRequiredService<IControlFactory>();

        var label1 = controlFactory.Create("Label1", template: "Label",
            properties: new()
            {
                { "Text", @"""This app was created in .Net with YAML""" },
                { "Align", "Align.Center" },
                { "Width", "Parent.Width" }
            }
        );
        var button1 = controlFactory.Create("Button1", template: "Button",
            properties: new()
            {
                { "Text", @"""Click me!""" },
                { "Align", "Align.Center" },
                { "Width", "Parent.Width" }
            }
        );

        var groupContainer = controlFactory.Create("GroupContainer", template: "GroupContainer",
            properties: new Dictionary<string, ControlPropertyValue>()
            {
                { "Width", new() { Value = "App.Width" } }
            },
            children: [label1, button1]
        );

        var graph = controlFactory.CreateScreen("Hello from .Net",
            children: [groupContainer]
        );
        var app = controlFactory.CreateApp(appName);
        app.Screens.Add(graph);

        return app;
    }

    private static void Main(string[] args)
    {
        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        var fullPathToMsApp = args.Length > 0 ? args[0] : null;
        if (fullPathToMsApp == null)
        {
            Console.WriteLine("No args provided, using default file path");
            fullPathToMsApp = Path.Combine(Directory.GetCurrentDirectory(), "CanvasApp.msapp");
            Console.WriteLine(fullPathToMsApp);
        }

        if (File.Exists(fullPathToMsApp)) // Overwrite
        {
            Console.WriteLine("Warning: File already exists;  Overwrite? (y / n)");
            var input = Console.ReadLine();
            if (input?.ToLower()[0] == 'y') File.Delete(fullPathToMsApp);
        }

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        // Add a basic example app (note: this will be replaced with interactive process)
        msapp.App = GetExampleApp(provider, Path.GetFileNameWithoutExtension(fullPathToMsApp));

        // Output the MSApp to the path provided
        msapp.Save();
    }
}
