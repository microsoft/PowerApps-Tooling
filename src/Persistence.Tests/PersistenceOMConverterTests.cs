// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Persistence.Tests;

[TestClass]
public class PersistenceOMConverterTests : VSTestBase
{
    #region RoundTrip from yaml

    [TestMethod]
    [DataRow(@"_TestData/SchemaV2_2/Examples/Src/App.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/Examples/Src/Screens/Screen1.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/Examples/Src/Screens/FormsScreen2.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/Examples/Src/Screens/ComponentsScreen4.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/Examples/Src/Components/MyHeaderComponent.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/Examples/Single-File-App.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/FullSchemaUses/App.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/FullSchemaUses/Screens-general-controls.pa.yaml")]
    [DataRow(@"_TestData/SchemaV2_2/FullSchemaUses/Screens-with-components.pa.yaml")]
    public void RoundTripFromYaml(string path)
    {
        // Setup
        var (controlTemplateStore, controlFactory) = SetupServices();
        var converter = new PersistenceOMConverter(controlFactory, controlTemplateStore);

        // Deserialize
        var originalYaml = File.ReadAllText(path);
        var paFileRoot = PaYamlSerializer.Deserialize<PaFileRoot>(originalYaml);
        paFileRoot.ShouldNotBeNull();

        // Transform ToPersistenceOM
        var persistenceOM = converter.FromPaYamlFileRoot(paFileRoot);

        // Transform ToPaYaml
        var roundTrippedPaFileRoot = converter.ToPaYamlFileRoot(persistenceOM.App, persistenceOM.ComponentDefinitions, persistenceOM.Screens);

        // Serialize
        var roundTrippedYaml = PaYamlSerializer.Serialize(roundTrippedPaFileRoot);

        // Verify
        TestContext.WriteTextWithLineNumbers(roundTrippedYaml, "roundTrippedYaml:");
        roundTrippedYaml.Should().BeYamlEquivalentTo(originalYaml);
    }

    private static (IControlTemplateStore ControlTemplateStore, IControlFactory ControlFactory) SetupServices()
    {
        var services = TestBase.BuildServiceProviderForPersistenceTests(store =>
            {
                RegisterBuiltInCapabilityTemplates(store);
                RegisterWellKnownControlTemplates(store);
                RegisterAppTestingTemplates(store);
                RegisterTestPcfControlTemplates(store);
            });

        var controlTemplateStore = services.GetRequiredService<IControlTemplateStore>();
        var controlFactory = services.GetRequiredService<IControlFactory>();

        return (controlTemplateStore, controlFactory);
    }

    /// <summary>
    /// Add the control templates for built-in first class capabilities provided by the Canvas app.
    /// </summary>
    /// <param name="controlTemplateStore"></param>
    private static void RegisterBuiltInCapabilityTemplates(IControlTemplateStore controlTemplateStore)
    {
        // These are already done by AddMinimalTemplates:
        //controlTemplateStore.Add(new(WellKnownTemplateIds.appinfo));
        //controlTemplateStore.Add(new(WellKnownTemplateIds.hostcontrol));
        //controlTemplateStore.Add(new(WellKnownTemplateIds.screen));

        // These don't work atm:
        //// These are not actual controls
        //controlTemplateStore.Add(new(WellKnownTemplateIds.Component));
        //controlTemplateStore.Add(new(WellKnownTemplateIds.CommandComponent));
        //controlTemplateStore.Add(new(WellKnownTemplateIds.DataComponent));
        //controlTemplateStore.Add(new(WellKnownTemplateIds.FunctionComponent));
    }

    /// <summary>
    /// Add the known control templates from the product library.
    /// These were extracted from the PA-Client repo, and may not represent the full set of possible controls.
    /// </summary>
    /// <param name="controlTemplateStore"></param>
    private static void RegisterAppTestingTemplates(IControlTemplateStore controlTemplateStore)
    {
        controlTemplateStore.Add(new(WellKnownTemplateIds.TestSuite));
        controlTemplateStore.Add(new(WellKnownTemplateIds.AppTest));
        controlTemplateStore.Add(new(WellKnownTemplateIds.TestCase));
    }

    /// <summary>
    /// Add the known control templates from the product library.
    /// </summary>
    /// <param name="controlTemplateStore"></param>
    private static void RegisterWellKnownControlTemplates(IControlTemplateStore controlTemplateStore)
    {
        // These were extracted from the PA-Client repo, and may not represent the full set of possible controls.
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/addMedia"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/attachments"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/barcode"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/button"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/card"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/combobox"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/conditional"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/datatable"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/datepicker"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/dropdown"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/entityForm"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/fluidGrid"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/form"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/formViewer"));
        //controlTemplateStore.Add(new("http://microsoft.com/appmagic/gallery"));
        //controlTemplateStore.Add(new("http://microsoft.com/appmagic/group"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/groupContainer"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/htmlViewer"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/icon"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/image"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/label"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/lookup"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/pdfViewer"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/radio"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/rating"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/richTextEditorControl"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/shapes/circle"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/shapes/rectangle"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/shapes/triangle"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/slider"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/text"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/timer"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/toggleSwitch"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/videoPlayback"));
    }

    private static void RegisterTestPcfControlTemplates(IControlTemplateStore controlTemplateStore)
    {
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas"));
        controlTemplateStore.Add(new("http://microsoft.com/appmagic/powercontrol/TestControl"));
    }

    #endregion
}
