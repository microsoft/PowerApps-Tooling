// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using MSAppGenerator;

namespace MauiMsApp;

public partial class CreatePage : ContentPage
{
    public CreatePage()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Attempt to do specified app creation
    /// </summary>
    private void CreateMSApp(bool interactive, string fullPathToMsApp, int numScreens, IList<string>? controlsinfo)
    {
        // Setup services for creating MSApp representation
        var provider = Handler!.MauiContext!.Services;

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        // Select Generator based off specified mode
        var generator = provider.GetRequiredService<IAppGeneratorFactory>().Create(interactive);

        // Generate the app
        msapp.App = generator.GenerateApp(Path.GetFileNameWithoutExtension(fullPathToMsApp),
                numScreens, controlsinfo);

        // Output the MSApp to the path provided
        msapp.Save();
        Console.WriteLine("Success!  MSApp generated and saved to the provided path");
    }

#pragma warning disable CA1822 // Mark members as static
    private async void OnCreateClicked(object sender, EventArgs e)
#pragma warning restore CA1822 // Mark members as static
    {
        try
        {
            var filePath = _filePathEntry.Text;
            int numScreens;
            var result = int.TryParse(_numScreensEntry.Text, out numScreens);
            var controlTemplates = _controlTemplatesEntry.Text.Split(' ');
            CreateMSApp(false, filePath, numScreens, controlTemplates);
        }
        catch (Exception)
        {
            // The user canceled or something went wrong
        }
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}

