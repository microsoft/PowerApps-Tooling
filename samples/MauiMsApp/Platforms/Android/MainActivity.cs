// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MauiMsApp;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
