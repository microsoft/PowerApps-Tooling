// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;

internal class GlobalTemplates
{
    public static void AddCodeOnlyTemplates(TemplateStore templateStore, Dictionary<string, ControlTemplate> templates, AppType type)
    {
        templates.Add("appinfo", CreateAppInfoTemplate(type));
        if (!templateStore.TryGetTemplate("appinfo", out _))
        {
            templateStore.AddTemplate("appinfo", new CombinedTemplateState()
            {
                Id = "http://microsoft.com/appmagic/appinfo",
                Name = "appinfo",
                Version = "1.0",
                LastModifiedTimestamp = "0",
                IsComponentTemplate = false,
                FirstParty = true,
            });
        }

        templates.Add("screen", CreateScreenTemplate(type));
        if (!templateStore.TryGetTemplate("screen", out _))
        {
            templateStore.AddTemplate("screen", new CombinedTemplateState()
            {
                Id = "http://microsoft.com/appmagic/screen",
                Name = "screen",
                Version = "1.0",
                LastModifiedTimestamp = "0",
                IsComponentTemplate = false,
                FirstParty = true,
            });
        }

        /*  Prior to PAC Document version 1.318, the groupContainer control was not version
            flexible, so we must check for its existence here. These can be removed from
            GlobalTemplates IFF all documents are upgraded to 1.318+ (unlikely) OR if support
            for documents 1.317 and lower is dropped. */
        if (!templates.ContainsKey("groupContainer"))
        {
            templates.Add("groupContainer", CreateGroupContainerTemplate(type));
        }
        if (!templateStore.TryGetTemplate("groupContainer", out _))
        {
            templateStore.AddTemplate("groupContainer", new CombinedTemplateState()
            {
                Id = "http://microsoft.com/appmagic/groupContainer",
                Name = "groupContainer",
                Version = "1.0",
                LastModifiedTimestamp = "0",
                IsComponentTemplate = false,
                FirstParty = true,
            });
        }
    }

    private static ControlTemplate CreateAppInfoTemplate(AppType type)
    {
        var template = new ControlTemplate("appinfo", "1.0", "http://microsoft.com/appmagic/appinfo");
        template.InputDefaults.Add("MinScreenHeight", type == AppType.Phone ? "640" : "320");
        template.InputDefaults.Add("MinScreenWidth", type == AppType.Phone ? "640" : "320");
        template.InputDefaults.Add("ConfirmExit", "false");
        template.InputDefaults.Add("SizeBreakpoints", type == AppType.Phone ? "[1200, 1800, 2400]" : "[600, 900, 1200]");
        return template;
    }

    private static ControlTemplate CreateScreenTemplate(AppType type)
    {
        var template = new ControlTemplate("screen", "1.0", "http://microsoft.com/appmagic/screen");
        template.InputDefaults.Add("Fill", "RGBA(255, 255, 255, 1)");
        template.InputDefaults.Add("Height", "Max(App.Height, App.MinScreenHeight)");
        template.InputDefaults.Add("Width", "Max(App.Width, App.MinScreenWidth)");
        template.InputDefaults.Add("Size", "1 + CountRows(App.SizeBreakpoints) - CountIf(App.SizeBreakpoints, Value >= Self.Width)");
        template.InputDefaults.Add("Orientation", "If(Self.Width < Self.Height, Layout.Vertical, Layout.Horizontal)");
        template.InputDefaults.Add("LoadingSpinner", "LoadingSpinner.None");
        template.InputDefaults.Add("LoadingSpinnerColor", "RGBA(0, 51, 102, 1)");
        template.InputDefaults.Add("ImagePosition", "ImagePosition.Fit");
        return template;
    }

    private static ControlTemplate CreateGroupContainerTemplate(AppType type)
    {
        var template = new ControlTemplate("groupContainer", "1.0", "http://microsoft.com/appmagic/groupContainer");
        template.InputDefaults.Add("Width", "500");
        template.InputDefaults.Add("Height", type == AppType.Phone ? "225" : "200");
        template.InputDefaults.Add("PaddingTop", "0");
        template.InputDefaults.Add("PaddingBottom", "0");
        template.InputDefaults.Add("PaddingRight", "0");
        template.InputDefaults.Add("PaddingLeft", "0");
        template.InputDefaults.Add("BorderColor", "RGBA(0, 0, 0, 1)");
        template.InputDefaults.Add("BorderStyle", "BorderStyle.Solid");
        template.InputDefaults.Add("BorderThickness", "0");
        template.InputDefaults.Add("Fill", "RGBA(0, 0, 0, 0)");
        template.InputDefaults.Add("X", "0");
        template.InputDefaults.Add("Y", "0");
        template.InputDefaults.Add("Visible", "true");
        template.InputDefaults.Add("ChildTabPriority", "true");
        template.InputDefaults.Add("EnableChildFocus", "true");


        // These share a lot of properties, and they should be in the base template, but this is
        // mirroring what is found in the groupContainer_oam.xml definition. 
        var horizontalContainerDefaults = new Dictionary<string, string>()
        {
            { "LayoutMode", "LayoutMode.Auto" },
            { "LayoutDirection", "LayoutDirection.Horizontal" },
            { "LayoutWrap", "false" },
            { "LayoutAlignItems", "LayoutAlignItems.Start" },
            { "LayoutJustifyContent", "LayoutJustifyContent.Start" },
            { "LayoutGap", "0" },
            { "LayoutOverflowX", "LayoutOverflow.Hide" },
            { "LayoutOverflowY", "LayoutOverflow.Hide" }
        };

        template.VariantDefaultValues.Add("horizontalAutoLayoutContainer", horizontalContainerDefaults);

        var verticalContainerDefaults = new Dictionary<string, string>()
        {
            { "LayoutMode", "LayoutMode.Auto" },
            { "LayoutDirection", "LayoutDirection.Vertical" },
            { "LayoutWrap", "false" },
            { "LayoutAlignItems", "LayoutAlignItems.Start" },
            { "LayoutJustifyContent", "LayoutJustifyContent.Start" },
            { "LayoutGap", "0" },
            { "LayoutOverflowX", "LayoutOverflow.Hide" },
            { "LayoutOverflowY", "LayoutOverflow.Hide" }
        };
        template.VariantDefaultValues.Add("verticalAutoLayoutContainer", verticalContainerDefaults);

        var manualContainerDefaults = new Dictionary<string, string>()
        {
            { "LayoutMode", "LayoutMode.Auto" },
            { "LayoutDirection", "LayoutDirection.Manual" },
            { "LayoutWrap", "false" },
            { "LayoutAlignItems", "LayoutAlignItems.Start" },
            { "LayoutJustifyContent", "LayoutJustifyContent.Start" },
            { "LayoutGap", "0" },
            { "LayoutOverflowX", "LayoutOverflow.Hide" },
            { "LayoutOverflowY", "LayoutOverflow.Hide" }
        };
        template.VariantDefaultValues.Add("manualLayoutContainer", manualContainerDefaults);

        return template;
    }
}
