// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace MauiMsApp;

public class ScreensViewModel : ObservableCollection<ScreensViewModel>
{
    public ScreensViewModel(IMsappArchive msappArchive)
    {
    }
}
