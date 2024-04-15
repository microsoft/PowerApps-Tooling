// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using MSAppGenerator;

namespace MauiMsApp;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .RegisterMsAppPersistence()
            .RegisterPages()
            .RegisterViewModels()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static MauiAppBuilder RegisterMsAppPersistence(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddPowerAppsPersistence(store => store.TESTING_ONLY_AddDefaultTemplates());
        mauiAppBuilder.Services.AddTransient<IAppGeneratorFactory, AppGeneratorFactory>();

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterPages(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddTransient<MainPage>();
        mauiAppBuilder.Services.AddTransient<ScreensPage>();
        mauiAppBuilder.Services.AddTransient<CreatePage>();

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddTransient<ScreensViewModel>();

        return mauiAppBuilder;
    }
}
