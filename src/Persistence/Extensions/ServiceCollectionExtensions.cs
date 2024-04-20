// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// registers the MSAPP persistence services
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    /// <param name="useDefaultTemplates">if true, registers the default templates (eg. 'text', 'button') on the templates store.</param>
    public static void AddPowerAppsPersistence(this IServiceCollection services, bool useDefaultTemplates = false)
    {
        services.AddSingleton<IMsappArchiveFactory, MsappArchiveFactory>();
        services.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        services.AddSingleton<IControlFactory, ControlFactory>();

        services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(ctx =>
        {
            var store = new ControlTemplateStore();

            AddMinimalTemplates(store);

            if (useDefaultTemplates)
                AddDefaultTemplates(store);

            store.DiscoverBuiltInTemplateTypes();

            return store;
        });
    }

    private static void AddMinimalTemplates(ControlTemplateStore store)
    {
        store.Add(new() { InvariantName = "hostControl", DisplayName = "host", Id = BuiltInTemplates.Host.Id });
        store.Add(new() { InvariantName = "appinfo", DisplayName = "app", Id = BuiltInTemplates.App.Id });

        store.Add(new() { InvariantName = "AppTest", DisplayName = "AppTest", Id = BuiltInTemplates.AppTest.Id });
        store.Add(new() { InvariantName = "TestSuite", DisplayName = "TestSuite", Id = BuiltInTemplates.TestSuite.Id });
        store.Add(new() { InvariantName = "TestCase", DisplayName = "TestCase", Id = BuiltInTemplates.TestCase.Id });

        store.Add(new() { InvariantName = "screen", Id = BuiltInTemplates.Screen.Id });
        store.Add(new() { InvariantName = "component", Id = BuiltInTemplates.Component.Id });
        store.Add(new() { InvariantName = "group", Id = BuiltInTemplates.Group.Id });

        // Gallery
        store.Add(new()
        {
            InvariantName = "gallery",
            Id = "http://microsoft.com/appmagic/gallery",
            NestedTemplates = new ControlTemplate[]
            {
                new()
                {
                    InvariantName = "galleryTemplate",
                    Id = "http://microsoft.com/appmagic/galleryTemplate",
                    AddPropertiesToParent = true,
                    InputProperties =
                    {
                        { "ItemAccessibleLabel", string.Empty },
                        { "TemplateFill", string.Empty },
                        { "OnSelect", string.Empty }
                    }
                }
            }
        });
        store.Add(new() { InvariantName = "commandComponent", Id = "http://microsoft.com/appmagic/CommandComponent" });
    }

    /// <summary>
    /// Adds some default templates which are useful for testing
    /// </summary>
    /// <param name="store"></param>
    private static void AddDefaultTemplates(ControlTemplateStore store)
    {
        // Classic templates
        store.Add(new() { InvariantName = "text", Id = "http://microsoft.com/appmagic/text", IsClassic = true });
        store.Add(new() { InvariantName = "button", Id = "http://microsoft.com/appmagic/button", IsClassic = true });
        store.Add(new() { InvariantName = "label", Id = "http://microsoft.com/appmagic/label", IsClassic = true });

        store.Add(new() { InvariantName = "button", Id = "http://microsoft.com/appmagic/powercontrol/Microsoft_CoreControls_Button" });

        // Modern/PCF templates
        store.Add(new() { InvariantName = "TextCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas" });
        store.Add(new() { InvariantName = "ButtonCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas" });

        store.Add(new() { InvariantName = "DataCard", Id = "http://microsoft.com/appmagic/card" });
        store.Add(new() { InvariantName = "TypedDataCard", Id = "http://microsoft.com/appmagic/card" });

        store.Add(new() { InvariantName = "groupContainer", Id = "http://microsoft.com/appmagic/groupContainer" });
    }
}
