using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace MauiMsApp;

public partial class ScreensPage : ContentPage
{
    IMsappArchive? _msappArchive;

    public ScreensPage()
    {
        InitializeComponent();
    }

    public required IMsappArchive? MsappArchive
    {
        get => _msappArchive;
        set
        {
            _msappArchive = value;
            Title = _msappArchive?.App?.Name;
            _screens.ItemsSource = _msappArchive?.App?.Screens;
            OnPropertyChanged(nameof(MsappArchive));
        }
    }
}
