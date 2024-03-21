// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace MSAppGenerator;

/// <summary>
/// Generates an MSApp using input from interactive commandline session
/// </summary>
public class InteractiveGenerator : IAppGenerator
{
    private readonly IControlFactory _controlFactory;

    public InteractiveGenerator(IControlFactory controlFactory)
    {
        _controlFactory = controlFactory;
    }

    /// <summary>
    /// Simple function to shorthand the 'console write, console read' paradigm
    /// </summary>
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

    /// <summary>
    /// Performs interactive commandline session to specify parameters for an app
    /// </summary>
    public App GenerateApp(string filePath, int numScreens, IList<string>? controls)
    {
        var app = _controlFactory.CreateApp(filePath);

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

                        childlist.Add(_controlFactory.Create(controlname, template: controltemplate,
                            properties: new()
                        ));
                    }
                    else if (input?.ToLower()[0] == 'n')
                    {
                        continueScreen = false;
                        app.Screens.Add(_controlFactory.CreateScreen(screenname, children: childlist.ToArray()));
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
