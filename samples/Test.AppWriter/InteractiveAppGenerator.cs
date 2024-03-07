// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Test.AppWriter;
internal class InteractiveAppGenerator
{
    // Simple function to shorthand the 'console write, console read' paradigm
    private static string Prompt(string prompt)
    {
        string? input = null;
        while (input == null)
        {
            Console.Write(prompt);
            input = Console.ReadLine();
        }
        return input;
    }

    // Performs interactive commandline session to specify parameters for an app
    public static App GenerateApp(IServiceProvider provider, string appName)
    {
        var controlFactory = provider.GetRequiredService<IControlFactory>();
        var app = controlFactory.CreateApp(appName);

        string input;
        var continueApp = true;
        while (continueApp)
        {
            input = Prompt("Create new Screen? (y/n): ");
            if (input?.ToLower()[0] == 'y')
            {
                var screenname = Prompt("  Input screen name: ");

                var childlist = new List<Control>();
                var continueScreen = true;
                while (continueScreen)
                {
                    input = Prompt("  Create new Control? (y/n): ");
                    if (input?.ToLower()[0] == 'y')
                    {
                        var controlname = Prompt("    Input control name: ");
                        var controltemplate = Prompt("    Input control template: ");
                        //input = Prompt("Specify properties? (y/n): ");

                        childlist.Add(controlFactory.Create(controlname, template: controltemplate,
                            properties: new()
                        ));
                    }
                    else if (input?.ToLower()[0] == 'n')
                    {
                        continueScreen = false;
                        app.Screens.Add(controlFactory.CreateScreen(screenname, children: childlist.ToArray()));
                    }
                }

            }
            else if (input?.ToLower()[0] == 'n')
            {
                input = Prompt("End app creation? (y/n): ");

                if (input?.ToLower()[0] == 'y')
                {
                    continueApp = false;
                }
            }
        }

        return app;
    }
}
