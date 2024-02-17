// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

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
                { "Align", "Align.Center" },
                { "BorderColor", "RGBA(0, 18, 107, 1)" },
                { "Color", "RGBA(0, 0, 0, 1)" },
                { "DisabledColor", "RGBA(166, 166, 166, 1)" },
                { "Height", "100" },
                { "PaddingBottom", "20" },
                { "PaddingTop", "20" },
                { "Size", "26" },
                { "ZIndex", "1" },
                { "Text", @"""This app was created in .Net with YAML""" },
                { "Width", "Parent.Width" }
            }
        );
        var button1 = controlFactory.Create("Button1", template: "Button",
            properties: new()
            {
                { "DisabledBorderColor", "RGBA(166, 166, 166, 1)" },
                { "DisabledColor", "RGBA(166, 166, 166, 1)" },
                { "DisabledFill", "RGBA(244, 244, 244, 1)" },
                { "Fill", "RGBA(56, 96, 178, 1)" },
                { "FontWeight", "FontWeight.Semibold" },
                { "HoverColor", "RGBA(255, 255, 255, 1)" },
                { "HoverFill", "ColorFade(RGBA(56, 96, 178, 1), -20 %)" },
                { "OnSelect", @"Notify(""Thank you"", NotificationType.Success)" },
                { "Size", "15" },
                { "Text", @"""Click me!""" },
                { "ZIndex", "2" },
            }
        );

        var groupContainer = controlFactory.Create("GroupContainer", template: "GroupContainer",
            properties: new()
            {
                { "DropShadow", "DropShadow.Light" },
                { "LayoutAlignItems", "LayoutAlignItems.Center" },
                { "LayoutDirection", "LayoutDirection.Vertical" },
                { "LayoutJustifyContent", "LayoutJustifyContent.Center" },
                { "LayoutMode", "LayoutMode.Auto" },
                { "RadiusBottomLeft", "4" },
                { "RadiusBottomRight", "4" },
                { "RadiusTopLeft", "4" },
                { "RadiusTopRight", "4" },
                { "Width", "App.Width" },
                { "ZIndex", "1" },
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
