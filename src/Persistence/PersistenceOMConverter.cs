// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using static Microsoft.PowerPlatform.PowerApps.Persistence.Models.CustomProperty;
using PComponentDefinition = Microsoft.PowerPlatform.PowerApps.Persistence.Models.ComponentDefinition;
using YComponentDefinition = Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2.ComponentDefinition;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

/// <summary>
/// Converts the persistence object model (<see cref="Models"/>) to/from the PaYaml object model (<see cref="PaYaml.Models"/>).
/// </summary>
public class PersistenceOMConverter
{
    private const string AppControlName = "App";
    private const string HostControlName = "Host";
    private const string HostControlTemplateName = "HostControl";
    private const string ComponentInstanceControlType = "component";
    private const string PcfControlInstanceTemplateName = "???";

    private readonly IControlFactory _controlFactory;
    private readonly IControlTemplateStore _controlTemplateStore;

    public PersistenceOMConverter(IControlFactory controlFactory, IControlTemplateStore controlTemplateStore)
    {
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    #region FromPaYaml*

    public (App? App, ICollection<Screen> Screens, ICollection<PComponentDefinition> ComponentDefinitions) FromPaYamlFileRoot(PaFileRoot paFileRoot)
    {
        _ = paFileRoot ?? throw new ArgumentNullException(nameof(paFileRoot));

        // The order of loading sections of the yaml is important for dependency resolution.
        var pApp = paFileRoot.App == null ? null : FromPaYaml(paFileRoot.App);

        // Need to load component definitions before loading control instances
        var pComponentDefs = paFileRoot.ComponentDefinitions?.Select(FromPaYaml).ToArray() ?? Array.Empty<PComponentDefinition>();

        var pScreens = paFileRoot.Screens?.Select(FromPaYaml).ToArray() ?? Array.Empty<Screen>();

        return (pApp, pScreens, pComponentDefs);
    }

    private App FromPaYaml(AppInstance yApp)
    {
        var pHostProperties = yApp.Children?.Host is not null && yApp.Children.Host.Properties.Count > 0
            ? yApp.Children.Host.Properties.Select(FromPaYaml)
            : Enumerable.Empty<ControlProperty>();

        return new App(AppControlName, variant: string.Empty, _controlTemplateStore)
        {
            Properties = new(yApp.Properties.Select(FromPaYaml)),
            Children = new Control[]
            {
              _controlFactory.Create(HostControlName, HostControlTemplateName, properties: new(pHostProperties))
            },
        };
    }

    private Screen FromPaYaml(NamedObject<ScreenInstance> yScreen)
    {
        return new(yScreen.Name, variant: string.Empty, _controlTemplateStore)
        {
            Properties = new(yScreen.Value.Properties.Select(FromPaYaml)),
            Children = yScreen.Value.Children.Select(FromPaYaml).ToList(),
        };
    }

    public Control FromPaYaml(NamedObject<ControlInstance> yControl)
    {
        var template = ResolveControlTemplate(yControl);

        // Otherwise, assume generic control instance
        var pProperties = yControl.Value.Properties.Select(FromPaYaml);
        var pChildren = yControl.Value.Children.Select(FromPaYaml);
        return _controlFactory.Create(yControl.Name, template,
            variant: yControl.Value.Variant,
            properties: new(pProperties),
            children: pChildren.ToList());
    }

    private ControlTemplate ResolveControlTemplate(NamedObject<ControlInstance> yControl)
    {
        if (yControl.Value.ControlType == ComponentInstanceControlType)
        {
            //var componentName = yControl.Value.ComponentName.EmptyToNull() ?? throw new PaYamlSerializationException($"The control named {yControl.Name} is missing a value for {nameof(yControl.Value.ComponentName)}.", yControl.Start);
            //var componentLibraryUniqueName = yControl.Value.ComponentName.EmptyToNull();

            // TODO: Utilize the componentName and componentLibraryUniqueName to resolve the actual template name
            throw ErrorControlInstanceNotImplementedYet(yControl);
        }
        else if (yControl.Value.ControlType == PcfControlInstanceTemplateName)
        {
            throw ErrorControlInstanceNotImplementedYet(yControl);
        }
        else
        {
            if (!_controlTemplateStore.TryGetTemplateByName(yControl.Value.ControlType, out var controlTemplate))
            {
                throw new PaYamlSerializationException($"The control type '{yControl.Value.ControlType}' could not be found for control named '{yControl.Name}'.", yControl.Start);
            }

            return controlTemplate;
        }
    }

    private PComponentDefinition FromPaYaml(NamedObject<YComponentDefinition> yComponentDef)
    {
        var controlTemplate = RetrieveComponentDefinition();
        return new(yComponentDef.Name, variant: string.Empty, controlTemplate)
        {
            Description = yComponentDef.Value.Description,
            AccessAppScope = yComponentDef.Value.AccessAppScope,
            CustomProperties = yComponentDef.Value.CustomProperties.Select(FromPaYamlCustomProperty).ToList(),
            Properties = new(yComponentDef.Value.Properties.Select(FromPaYaml)),
            Children = yComponentDef.Value.Children.Select(FromPaYaml).ToList(),
        };

        static ControlTemplate RetrieveComponentDefinition()
        {
            throw new NotImplementedException($"Creating a ComponentDefinition in the PersistenceOM is not clear. Please implement.");
        }
    }

    private CustomProperty FromPaYamlCustomProperty(NamedObject<ComponentCustomPropertyUnion> yCustomProperty)
    {
        return yCustomProperty.Value.PropertyKind switch
        {
            ComponentPropertyKind.Input => FromPaYamlCustomInputProperty(),
            ComponentPropertyKind.Output => FromPaYamlCustomOutputProperty(),
            ComponentPropertyKind.InputFunction => FromPaYamlCustomInputFunctionProperty(),
            ComponentPropertyKind.OutputFunction => FromPaYamlCustomOutputFunctionProperty(),
            ComponentPropertyKind.Action => FromPaYamlCustomActionProperty(),
            ComponentPropertyKind.Event => FromPaYamlCustomEventProperty(),
            _ => throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' has an unsupported value for {nameof(yCustomProperty.Value.PropertyKind)} '{yCustomProperty.Value.PropertyKind}'.", yCustomProperty.Start)
        };

        CustomProperty FromPaYamlCustomInputProperty()
        {
            _ = yCustomProperty.Value.DataType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' is missing a value for {nameof(yCustomProperty.Value.DataType)}.", yCustomProperty.Start);

            return new()
            {
                Type = PropertyType.Data,
                Direction = PropertyDirection.Input,
                Name = yCustomProperty.Name,
                DisplayName = yCustomProperty.Value.DisplayName,
                Description = yCustomProperty.Value.Description,
                DataType = yCustomProperty.Value.DataType.Value.ToString(),
                IsResettable = yCustomProperty.Value.RaiseOnReset ?? false,
                Default = yCustomProperty.Value.Default?.InvariantScript,
            };
        }

        CustomProperty FromPaYamlCustomOutputProperty()
        {
            _ = yCustomProperty.Value.DataType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' is missing a value for {nameof(yCustomProperty.Value.DataType)}.", yCustomProperty.Start);

            return new()
            {
                Type = PropertyType.Data,
                Direction = PropertyDirection.Output,
                Name = yCustomProperty.Name,
                DisplayName = yCustomProperty.Value.DisplayName,
                Description = yCustomProperty.Value.Description,
                DataType = yCustomProperty.Value.DataType.Value.ToString(),
            };
        }

        CustomProperty FromPaYamlCustomInputFunctionProperty()
        {
            _ = yCustomProperty.Value.ReturnType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' is missing a value for {nameof(yCustomProperty.Value.ReturnType)}.", yCustomProperty.Start);

            return new()
            {
                Type = PropertyType.Function,
                Direction = PropertyDirection.Input,
                Name = yCustomProperty.Name,
                DisplayName = yCustomProperty.Value.DisplayName,
                Description = yCustomProperty.Value.Description,
                DataType = yCustomProperty.Value.ReturnType.Value.ToString(),
                Parameters = yCustomProperty.Value.Parameters.Select(FromPaYaml).ToList(),
                Default = yCustomProperty.Value.Default?.InvariantScript,
            };
        }

        CustomProperty FromPaYamlCustomOutputFunctionProperty()
        {
            _ = yCustomProperty.Value.ReturnType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' is missing a value for {nameof(yCustomProperty.Value.ReturnType)}.", yCustomProperty.Start);

            return new()
            {
                Type = PropertyType.Function,
                Direction = PropertyDirection.Output,
                Name = yCustomProperty.Name,
                DisplayName = yCustomProperty.Value.DisplayName,
                Description = yCustomProperty.Value.Description,
                DataType = yCustomProperty.Value.ReturnType.Value.ToString(),
                Parameters = yCustomProperty.Value.Parameters.Select(FromPaYaml).ToList(),
            };
        }

        CustomProperty FromPaYamlCustomActionProperty()
        {
            _ = yCustomProperty.Value.ReturnType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' is missing a value for {nameof(yCustomProperty.Value.ReturnType)}.", yCustomProperty.Start);

            return new()
            {
                Type = PropertyType.Action,
                Direction = PropertyDirection.Output,
                Name = yCustomProperty.Name,
                DisplayName = yCustomProperty.Value.DisplayName,
                Description = yCustomProperty.Value.Description,
                DataType = yCustomProperty.Value.ReturnType.Value.ToString(),
                Parameters = yCustomProperty.Value.Parameters.Select(FromPaYaml).ToList(),
            };
        }

        CustomProperty FromPaYamlCustomEventProperty()
        {
            _ = yCustomProperty.Value.ReturnType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' is missing a value for {nameof(yCustomProperty.Value.ReturnType)}.", yCustomProperty.Start);

            return new()
            {
                Type = PropertyType.Event,
                Direction = PropertyDirection.Input,
                Name = yCustomProperty.Name,
                DisplayName = yCustomProperty.Value.DisplayName,
                Description = yCustomProperty.Value.Description,
                DataType = yCustomProperty.Value.ReturnType.Value.ToString(),
                Parameters = yCustomProperty.Value.Parameters.Select(FromPaYaml).ToList(),
            };
        }

        CustomPropertyParameter FromPaYaml(NamedObject<PFxFunctionParameter> yFuncParameter)
        {
            _ = yFuncParameter.Value.DataType ?? throw new PaYamlSerializationException($"The custom property named '{yCustomProperty.Name}' has a parameter '{yFuncParameter.Name}' that is missing a value for {nameof(yFuncParameter.Value.DataType)}.", yFuncParameter.Start);

            return new()
            {
                Name = yFuncParameter.Name,
                Description = yFuncParameter.Value.Description,
                IsRequired = yFuncParameter.Value.IsRequired,
                DataType = yFuncParameter.Value.DataType.Value.ToString(),
            };
        }
    }

    private ControlProperty FromPaYaml(NamedObject<PFxExpressionYaml> yProp)
    {
        return new(yProp.Name, yProp.Value.InvariantScript);
    }

    #endregion

    #region ToPaYaml*

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design; transformer is intended to be called via DI.")]
    public PaFileRoot ToPaYamlFileRoot(
        App? pApp = null,
        IEnumerable<PComponentDefinition>? pComponentDefs = null,
        IEnumerable<Screen>? pScreens = null)
    {
        return new()
        {
            App = pApp is null ? null : ToPaYamlApp(pApp),
            ComponentDefinitions = new(pComponentDefs?.Select(ToPaYamlComponentDefinition)),
            Screens = new(pScreens?.Select(ToPaYamlScreen)),
        };
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design; transformer is intended to be called via DI.")]
    public NamedObjectSequence<ControlInstance> ToPaYamlControlInstances(ICollection<Control> controls)
    {
        return new(controls.Select(ToPaYamlControlInstance));
    }

    private static AppInstance ToPaYamlApp(App pApp)
    {
        return new()
        {
            Properties = new(pApp.Properties.Values.Select(ToPaYamlNamedObject)),
            Children = ToPaYamlAppChildren(pApp)
        };

        static AppInstanceChildren? ToPaYamlAppChildren(App pApp)
        {
            if (pApp.Children?.Count > 0)
            {
                HostControlInstance? yHost = null;
                foreach (var pChild in pApp.Children)
                {
                    if (pChild.Name == SchemaKeywords.Host)
                    {
                        AssertControlHasNoChildren(pChild);

                        yHost = new()
                        {
                            Properties = new(pChild.Properties.Values.Select(ToPaYamlNamedObject)),
                        };
                    }
                    else
                    {
                        throw new PersistenceTransformationException($"App has an unsupported child with name '{pChild.Name}'. Only a single child named '{SchemaKeywords.Host}' is currently allowed.");
                    }
                }

                return new()
                {
                    Host = yHost
                };
            }

            return null;
        }
    }

    private static NamedObject<ScreenInstance> ToPaYamlScreen(Screen pScreen)
    {
        return new(pScreen.Name, new()
        {
            Properties = new(pScreen.Properties.Values.Select(ToPaYamlNamedObject)),
            Children = new(pScreen.Children?.Select(ToPaYamlControlInstance)),
        });
    }

    private static NamedObject<YComponentDefinition> ToPaYamlComponentDefinition(PComponentDefinition pComponentDef)
    {
        // REVIEW: The current OM uses PComponentDefinition class for component defintions, commandComponent definitions, and component instances.
        // To remove ambiguity, we assert here the caller has passed in the supported kind.
        if (pComponentDef.TemplateId != WellKnownTemplateIds.Component)
        {
            throw new InvalidOperationException($"The component definition named '{pComponentDef.Name}' has a templateId of '{pComponentDef.TemplateId}' which is not supported. Only templateIds of '{BuiltInTemplates.Component}' are supported as component definitions.");
        }

        return new(pComponentDef.Name, new()
        {
            Description = pComponentDef.Description.EmptyToNull(),
            AccessAppScope = pComponentDef.AccessAppScope,
            CustomProperties = new(pComponentDef.CustomProperties.Select(ToPaYamlCustomProperty)),
            Properties = new(pComponentDef.Properties.Values.Select(ToPaYamlNamedObject)),
            Children = new(pComponentDef.Children?.Select(ToPaYamlControlInstance)),
        });
    }

    private static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomProperty(CustomProperty pCustomProp)
    {
        return (pCustomProp.Type, pCustomProp.Direction) switch
        {
            (PropertyType.Data, PropertyDirection.Input) => ToPaYamlCustomInputProperty(pCustomProp),
            (PropertyType.Data, PropertyDirection.Output) => ToPaYamlCustomOutputProperty(pCustomProp),
            (PropertyType.Function, PropertyDirection.Input) => ToPaYamlCustomInputFunction(pCustomProp),
            (PropertyType.Function, PropertyDirection.Output) => ToPaYamlCustomOutputFunction(pCustomProp),
            (PropertyType.Action, _) => ToPaYamlCustomAction(pCustomProp),
            (PropertyType.Event, _) => ToPaYamlCustomEvent(pCustomProp),
            _ => throw new NotImplementedException($"Custom property with name '{pCustomProp.Name}' has an unsupported combination of (Type: {pCustomProp.Type}, Direction: {pCustomProp.Direction}).")
        };

        static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomInputProperty(CustomProperty pCustomProp)
        {
            return new(pCustomProp.Name, new()
            {
                PropertyKind = ComponentPropertyKind.Input,
                DisplayName = pCustomProp.DisplayName.EmptyToNull(),
                Description = pCustomProp.Description.EmptyToNull(),
                DataType = ToPaYamlPFxEnum<PFxDataType>(pCustomProp.DataType),
                RaiseOnReset = pCustomProp.IsResettable,
                Default = ToPaYamlPFxExpressionOrNull(pCustomProp.Default),
            });
        }

        static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomOutputProperty(CustomProperty pCustomProp)
        {
            return new(pCustomProp.Name, new()
            {
                PropertyKind = ComponentPropertyKind.Output,
                DisplayName = pCustomProp.DisplayName.EmptyToNull(),
                Description = pCustomProp.Description.EmptyToNull(),
                DataType = ToPaYamlPFxEnum<PFxDataType>(pCustomProp.DataType),
            });
        }

        static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomInputFunction(CustomProperty pCustomProp)
        {
            return new(pCustomProp.Name, new()
            {
                PropertyKind = ComponentPropertyKind.InputFunction,
                DisplayName = pCustomProp.DisplayName.EmptyToNull(),
                Description = pCustomProp.Description.EmptyToNull(),
                ReturnType = ToPaYamlPFxEnum<PFxFunctionReturnType>(pCustomProp.DataType),
                Parameters = new(pCustomProp.Parameters.Select(ToPaYamlPFxFunctionParameter)),
                Default = ToPaYamlPFxExpressionOrNull(pCustomProp.Default),
            });
        }

        static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomOutputFunction(CustomProperty pCustomProp)
        {
            return new(pCustomProp.Name, new()
            {
                PropertyKind = ComponentPropertyKind.OutputFunction,
                DisplayName = pCustomProp.DisplayName.EmptyToNull(),
                Description = pCustomProp.Description.EmptyToNull(),
                ReturnType = ToPaYamlPFxEnum<PFxFunctionReturnType>(pCustomProp.DataType),
                Parameters = new(pCustomProp.Parameters.Select(ToPaYamlPFxFunctionParameter)),
            });
        }

        static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomAction(CustomProperty pCustomProp)
        {
            return new(pCustomProp.Name, new()
            {
                PropertyKind = ComponentPropertyKind.Action,
                DisplayName = pCustomProp.DisplayName.EmptyToNull(),
                Description = pCustomProp.Description.EmptyToNull(),
                ReturnType = ToPaYamlPFxEnum<PFxFunctionReturnType>(pCustomProp.DataType),
                Parameters = new(pCustomProp.Parameters.Select(ToPaYamlPFxFunctionParameter)),
            });
        }

        static NamedObject<ComponentCustomPropertyUnion> ToPaYamlCustomEvent(CustomProperty pCustomProp)
        {
            return new(pCustomProp.Name, new()
            {
                PropertyKind = ComponentPropertyKind.Event,
                DisplayName = pCustomProp.DisplayName.EmptyToNull(),
                Description = pCustomProp.Description.EmptyToNull(),
                ReturnType = ToPaYamlPFxEnum<PFxFunctionReturnType>(pCustomProp.DataType),
                Parameters = new(pCustomProp.Parameters.Select(ToPaYamlPFxFunctionParameter)),
            });
        }
    }

    private static NamedObject<PFxFunctionParameter> ToPaYamlPFxFunctionParameter(CustomPropertyParameter pParameter)
    {
        return new(pParameter.Name, new()
        {
            Description = pParameter.Description.EmptyToNull(),
            IsRequired = pParameter.IsRequired,
            DataType = ToPaYamlPFxEnum<PFxDataType>(pParameter.DataType),
        });
    }

    private static TEnum ToPaYamlPFxEnum<TEnum>(string pDataType)
         where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(pDataType))
            throw new ArgumentNullException(nameof(pDataType));

        if (!Enum.TryParse<TEnum>(pDataType, out var pfxDataType))
        {
            throw new PersistenceTransformationException($"Custom property has an unsupported enum value '{pDataType}' for type {typeof(TEnum).Name}.");
        }

        return pfxDataType;
    }

    private static PFxExpressionYaml? ToPaYamlPFxExpressionOrNull(string? script)
    {
        return script is null ? null : new(script);
    }

    private static NamedObject<ControlInstance> ToPaYamlControlInstance(Control pChild)
    {
        if (pChild.TemplateId is WellKnownTemplateIds.AppInfo
            or WellKnownTemplateIds.HostControl
            or WellKnownTemplateIds.Screen
            or WellKnownTemplateIds.TestSuite
            or WellKnownTemplateIds.TestCase
            or WellKnownTemplateIds.AppTest
            or WellKnownTemplateIds.CommandComponent
            or WellKnownTemplateIds.DataComponent
            or WellKnownTemplateIds.FunctionComponent
            )
        {
            throw ErrorInvalidPersistenceOMControlInstanceGraphControlNotAllowed(pChild);
        }

        if (pChild is PComponentDefinition)
        {
            throw ErrorInvalidPersistenceOMControlInstanceGraphControlNotAllowed(pChild);
        }

        if (pChild is ComponentInstance pComponentInstance)
        {
            if (pChild.TemplateId != WellKnownTemplateIds.Component)
            {
                throw new InvalidOperationException($"ComponentInstance.TemplateId MUST be equal to '{WellKnownTemplateIds.Component}'.");
            }

            // TODO: implement support for component instances
            throw ErrorControlInstanceNotImplementedYet(pChild);
        }

        ControlInstance yControlInstance = new()
        {
            ControlType = pChild.TemplateId,
            Variant = pChild.Variant.EmptyToNull(),
            Layout = pChild.Layout.EmptyToNull(),
            Properties = new(pChild.Properties.Values.Select(ToPaYamlNamedObject)),
            Children = new(pChild.Children?.Select(ToPaYamlControlInstance)),
        };

        return new(pChild.Name, yControlInstance);
    }

    private static void AssertControlHasNoChildren(Control pChild)
    {
        if (pChild.Children?.Count > 0)
        {
            throw new PersistenceTransformationException($"Control with name '{pChild.Name}' (TemplateId: {pChild.TemplateId}) has children, but this control is not expected to have children.");
        }
    }

    private static NamedObject<PFxExpressionYaml> ToPaYamlNamedObject(ControlProperty ctrlProp)
    {
        if (ctrlProp.IsFormula)
        {
            throw new PersistenceTransformationException($"Control property with name '{ctrlProp.Name}' has {nameof(ctrlProp.IsFormula)} set to true. Not sure if this needs to be handled differently here.");
        }

        if (ctrlProp.Value is null)
        {
            // What does it mean for an expression script to be null? For now, we will expect scripts to be set to empty string when explicitly setting a property script to be nothing.
            // REVIEW: We could also skip them if it's equivalent.
            throw new PersistenceTransformationException($"Control property with name '{ctrlProp.Name}' has a value of null, which is not supported. Set to the empty string or don't include it.");
        }

        return new(ctrlProp.Name, new(ctrlProp.Value));
    }

    #endregion

    private static InvalidOperationException ErrorInvalidPersistenceOMControlInstanceGraphControlNotAllowed(Control pChild)
    {
        return new InvalidOperationException($"The control instance named '{pChild.Name}' (type: {pChild.GetType().Name}, templateId: {pChild.TemplateId}) is not allowed in the control graph.");
    }

    private static NotImplementedException ErrorControlInstanceNotImplementedYet(Control pChild)
    {
        return new NotImplementedException($"The control instance named '{pChild.Name}' (type: {pChild.GetType().Name}, templateId: {pChild.TemplateId}) is not yet implemented.");
    }

    private static NotImplementedException ErrorControlInstanceNotImplementedYet(NamedObject<ControlInstance> yControl)
    {
        return new NotImplementedException($"The control instance named '{yControl.Name}' (Control type: {yControl.Value.ControlType}) is not yet implemented.");
    }
}

public static class PersistenceOMConverterExtensions
{
    public static PaFileRoot ToPaYamlRoot(this PersistenceOMConverter converter, Screen pScreen)
    {
        return converter.ToPaYamlFileRoot(pScreens: new[] { pScreen });
    }

    public static PaFileRoot ToPaYamlRoot(this PersistenceOMConverter converter, PComponentDefinition pComponentDef)
    {
        return converter.ToPaYamlFileRoot(pComponentDefs: new[] { pComponentDef });
    }
}
