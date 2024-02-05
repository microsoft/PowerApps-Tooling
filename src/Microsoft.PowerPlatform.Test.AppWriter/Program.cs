// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Test.AppWriter;

internal class Program
{
    private static App GetExampleApp(string appname, int numscreens = 1)
    {
        var app = new App(appname);

        for (var i = 0; i < numscreens; i++)
        {
            var graph = new Screen("Screen" + numscreens.ToString())
            {
                Properties = new Dictionary<string, ControlPropertyValue>()
                {
                    { "Text", new() { Value = "I am a screen" } },
                },
                Controls = new Control[]
                {
                    new Text("Label1")
                    {
                        Properties = new Dictionary<string, ControlPropertyValue>()
                        {
                            { "Text", new() { Value = "lorem ipsum" }  },
                        },
                    },
                    new Button("Button1")
                    {
                        Properties = new Dictionary<string, ControlPropertyValue>()
                        {
                            { "Text", new() { Value = "click me" }  },
                            { "X", new() { Value = "100" } },
                            { "Y", new() { Value = "200" } }
                        },
                    }
                }
            };

            app.Screens.Add(graph);
        }

        return app;
    }

    private static void Main(string[] args)
    {
        // Console.WriteLine(Directory.GetCurrentDirectory());
        // GetCommandLineArgs()
        var fullPathToMsApp = args.Length > 0 ? args[0] : null;

        if (fullPathToMsApp == null)
        {
            Console.WriteLine("No args provided, using default file path");
            fullPathToMsApp = Directory.GetCurrentDirectory() + "\\appname.msapp";
            Console.WriteLine(fullPathToMsApp);
        }

        if (File.Exists(fullPathToMsApp)) // Overwrite
        {
            Console.WriteLine("Warning: File already exists");
            // File.Delete(fullPathToMsApp);
        }

        //var writer = new StringWriter();
        //using var yamlWriter = new YamlWriter(writer);
        //using var serializer = new YamlPocoSerializer(yamlWriter);
        //serializer.Serialize(GetExampleApp());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var msappArchive = new MsappArchive(fullPathToMsApp, true, serviceProvider.GetRequiredService<IYamlSerializationFactory>());
        // msappArchive.CreateEntry(MsappArchive.Directories.Src + "/");
        // msappArchive.CreateEntry(MsappArchive.Directories.Controls + "/");
        // msappArchive.CreateEntry(MsappArchive.Directories.Components + "/");
        // msappArchive.CreateEntry(MsappArchive.Directories.AppTests + "/");
        // msappArchive.CreateEntry(MsappArchive.Directories.References + "/");
        // msappArchive.CreateEntry(MsappArchive.Directories.Resources + "/");

        var app = GetExampleApp("appname");
        msappArchive.App = app;
        msappArchive.Save();
    }
}
