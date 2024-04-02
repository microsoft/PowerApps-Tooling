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

            var creator = new AppCreator(Handler!.MauiContext!.Services);
            creator.CreateMSApp(false, filePath, numScreens, controlTemplates);

            await DisplayAlert("Success", "You are now a PowerApps Pro Developer!", "OK");
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
            await DisplayAlert("Alert", "Something went wrong: " + ex.Message, "OK");
        }
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}

