// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MauiMsApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}
