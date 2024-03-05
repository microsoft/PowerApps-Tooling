// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Test.AppWriter;
internal class ExampleAppGenerator
{
    public static List<(string, string)> ParseControlsInfo(string[] controlsinfo)
    {
        var tuples = new List<(string, string)>();

        for (var i = 0; i < controlsinfo.Length; i += 2)
        {
            tuples.Add((controlsinfo[i], controlsinfo[i + 1]));
        }

        return tuples;
    }

    // This produces a simple example app including the specified number of screens and a few generic controls
    // This is intended for testing purposes only
    public static App GetExampleApp(IServiceProvider provider, string appName, int numScreens)
    {
        var controlFactory = provider.GetRequiredService<IControlFactory>();

        var app = controlFactory.CreateApp(appName);

        for (var i = 0; i < numScreens; i++)
        {
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

            app.Screens.Add(graph);
        }

        return app;
    }

    // produce a specified app based on commandline input
    public static App CreateApp(IServiceProvider provider, string appName, int numScreens, List<(string, string)> controls)
    {
        var controlFactory = provider.GetRequiredService<IControlFactory>();

        var app = controlFactory.CreateApp(appName);

        for (var i = 0; i < numScreens; i++)
        {
            var childlist = new List<Control>();
            foreach (var control in controls)
            {
                childlist.Add(controlFactory.Create(control.Item1, template: control.Item2,
                    properties: new()
                ));
            }

            app.Screens.Add(controlFactory.CreateScreen("Hello from .Net", children: childlist.ToArray()));
        }

        return app;
    }
}