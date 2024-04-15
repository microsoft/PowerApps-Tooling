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
    /// <param name="additionalTemplateStoreConfiguration">
    /// When specified, performs additional configuration of the <see cref="ControlTemplateStore"/> instance.<br/>
    /// e.g. Tests could use the following to add test templates:<br/>
    /// <code>serviceCollection.AddPowerAppsPersistence(store => store.TESTING_ONLY_AddDefaultTemplates())</code>
    /// </param>
    public static void AddPowerAppsPersistence(
        this IServiceCollection services,
        Action<ControlTemplateStore>? additionalTemplateStoreConfiguration = null)
    {
        services.AddSingleton<IMsappArchiveFactory, MsappArchiveFactory>();
        services.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        services.AddSingleton<IControlFactory, ControlFactory>();

        services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(ctx =>
        {
            var store = new ControlTemplateStore();

            AddMinimalTemplates(store);

            store.DiscoverBuiltInTemplateTypes();
            additionalTemplateStoreConfiguration?.Invoke(store);

            return store;
        });
    }

    private static void AddMinimalTemplates(ControlTemplateStore store)
    {
        store.Add(new() { Name = "hostControl", DisplayName = "host", Id = BuiltInTemplates.Host.Id });
        store.Add(new() { Name = "appinfo", DisplayName = "app", Id = BuiltInTemplates.App.Id });
        store.Add(new() { Name = "screen", Id = BuiltInTemplates.Screen.Id });
        store.Add(new() { Name = "component", Id = BuiltInTemplates.Component.Id });
        store.Add(new() { Name = "group", Id = BuiltInTemplates.Group.Id });

        // Gallery
        store.Add(new()
        {
            Name = "gallery",
            Id = WellKnownTemplateIds.Gallery,
            NestedTemplates = new ControlTemplate[]
            {
                new()
                {
                    Name = "galleryTemplate",
                    Id = WellKnownTemplateIds.GalleryTemplate,
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
        store.Add(new() { Name = "commandComponent", Id = WellKnownTemplateIds.CommandComponent });
    }

    /// <summary>
    /// Adds some default templates which are useful for testing
    /// </summary>
    /// <param name="store"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "TEST ONLY function")]
    public static void TESTING_ONLY_AddDefaultTemplates(this ControlTemplateStore store)
    {
        _ = store ?? throw new ArgumentNullException(nameof(store));

        store.Add(new() { Name = "text", Id = "http://microsoft.com/appmagic/text" });
        store.Add(new() { Name = "button", Id = "http://microsoft.com/appmagic/button" });
        store.Add(new() { Name = "label", Id = "http://microsoft.com/appmagic/label" });

        store.Add(new() { Name = "TextCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas" });
        store.Add(new() { Name = "ButtonCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas" });

        store.Add(new() { Name = "DataCard", Id = "http://microsoft.com/appmagic/card" });
        store.Add(new() { Name = "TypedDataCard", Id = "http://microsoft.com/appmagic/card" });

        store.Add(new() { Name = "groupContainer", Id = "http://microsoft.com/appmagic/groupContainer" });
    }
}
