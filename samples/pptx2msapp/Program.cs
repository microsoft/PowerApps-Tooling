// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace pptx2msapp;

internal class Program
{
    public static IServiceProvider ServiceProvider { get; private set; }

    static Program()
    {
        ServiceProvider = ConfigureServiceProvider();
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Microsoft PowerPoint to Power Apps Canvas converter");
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: pptx2msapp <pptx file>");
            return;
        }

        // Check if the file exists and it is a pptx file
        if (!File.Exists(args[0]) || Path.GetExtension(args[0]) != ".pptx")
        {
            Console.WriteLine("Invalid file. Please provide a valid pptx file.");
            return;
        }

        // Convert the pptx file to a Power Apps Canvas app
        var pptxFilePath = args[0];
        var msappFilePath = Path.ChangeExtension(pptxFilePath, ".msapp");
        using var pptxConverter = ServiceProvider.GetRequiredService<PptxConverter>();
        pptxConverter.Convert(pptxFilePath, msappFilePath);

        Console.WriteLine($"Done: {msappFilePath}");
        Console.WriteLine();
    }

    /// <summary>
    /// Configures default services for generating the MSApp representation
    /// </summary>
    private static ServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        serviceCollection.AddTransient<PptxConverter>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }
}
