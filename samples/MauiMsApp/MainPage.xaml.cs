// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace MauiMsApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

#pragma warning disable CA1822 // Mark members as static
    private async void OnOpenClicked(object sender, EventArgs e)
#pragma warning restore CA1822 // Mark members as static
    {
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Please select a Power Apps file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { MsappArchive.MsappFileExtension } },
                }),
            };
            var result = await FilePicker.Default.PickAsync(options);
            if (result == null)
                return;

            var page = Handler!.MauiContext!.Services.GetRequiredService<ScreensPage>();
            var msappArchiveFactory = Handler!.MauiContext!.Services.GetRequiredService<IMsappArchiveFactory>();

            // Open Power Apps msapp file containing canvas app
            page.MsappArchive = msappArchiveFactory.Open(result.FullPath);

            await Navigation.PushAsync(page);
        }
        catch (Exception)
        {
            // The user canceled or something went wrong
        }
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        var page = Handler!.MauiContext!.Services.GetRequiredService<CreatePage>();
        await Navigation.PushAsync(page);
    }
}

