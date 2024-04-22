// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MSAppGenerator;

namespace MauiMsApp;

public partial class CreatePage : ContentPage
{
    public CreatePage()
    {
        InitializeComponent();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "REVIEW")]
    private async void OnCreateClicked(object sender, EventArgs e)
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

