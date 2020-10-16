// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class GalleryTemplateTransform : IControlTemplateTransform
    {
        public string TargetTemplate { get; } = "gallery";

        private const string _childTemplateName = "galleryTemplate";

        private readonly ControlInfoJson.Template _galleryTemplateJson;
        private readonly ControlTemplate _galleryTemplate;
        private Dictionary<string, ControlInfoJson.Item> _controlStore;
        private Theme _theme;

        public GalleryTemplateTransform(Dictionary<string, ControlTemplate> templateStore, Theme theme, Dictionary<string, ControlInfoJson.Item> controlStore)
        {
            templateStore.TryGetValue(_childTemplateName, out var template);
            _galleryTemplate = template;
            _galleryTemplateJson = ControlInfoJson.Template.CreateDefaultTemplate(_childTemplateName, _galleryTemplate);
            _controlStore = controlStore;
            _theme = theme;
        }

        public void AfterParse(ControlInfoJson.Item control)
        {
            // This will only be called on a control with the Gallery template.
            // If .studiostate was present, there will be a key for the child galleryTemplate;
            var galleryTemplateName = control.GalleryTemplateChildName;
            bool roundTrippedTemplate = true;

            control.GalleryTemplateChildName = null;
            ControlInfoJson.Item galleryTemplateChild;
            if (galleryTemplateName == null || !_controlStore.TryGetValue(galleryTemplateName, out galleryTemplateChild))
            {
                galleryTemplateChild = ControlInfoJson.Item.CreateDefaultControl(_galleryTemplate);
                if (galleryTemplateName == null)
                {
                    // create unambiguous name for gallery template control
                    var index = 1;
                    while (_controlStore.ContainsKey(control.Name + "template" + index))
                        index++;

                    galleryTemplateName = control.Name + "template" + index;
                }
                _controlStore.Add(galleryTemplateName, galleryTemplateChild);
                roundTrippedTemplate = false;
            }

            galleryTemplateChild.Name = galleryTemplateName;
            galleryTemplateChild.Template = _galleryTemplateJson;
            galleryTemplateChild.Parent = control.Name;
            galleryTemplateChild.Children = new ControlInfoJson.Item[0];

            var defaulter = new DefaultRuleHelper(galleryTemplateChild, _galleryTemplate, _theme);
            var defaults = defaulter.GetDefaultRules();

            // TODO: rules should be key value pairs in the ir, update this later
            var galleryTemplateRules = galleryTemplateChild.Rules.ToDictionary(rule => rule.Property);
            var parentCombinedRules = control.Rules.ToDictionary(rule => rule.Property);

            foreach (var rule in defaults)
            {
                var script = rule.Value;
                if (parentCombinedRules.TryGetValue(rule.Key, out var paScriptEntry))
                {
                    script = paScriptEntry.InvariantScript;
                    parentCombinedRules.Remove(rule.Key);
                }

                if (galleryTemplateRules.TryGetValue(rule.Key, out var entry))
                {
                    entry.InvariantScript = script;
                }
                else
                {
                    // For roundtripping, only rehydrate rules that existed on save
                    if (!roundTrippedTemplate)
                        galleryTemplateRules.Add(rule.Key, new ControlInfoJson.RuleEntry() { Property = rule.Key, InvariantScript = rule.Value });
                }
            }

            control.Rules = parentCombinedRules.Values.ToArray();
            galleryTemplateChild.Rules = galleryTemplateRules.Values.ToArray();

            control.Children = control.Children.Prepend(galleryTemplateChild).OrderBy(item => item.PublishOrderIndex).ToArray();
        }

        public void BeforeWrite(ControlInfoJson.Item control)
        {
            // This will only be called on a control with the Gallery template.
            // If it was from a valid .msapp, there will be a child control with `galleryTemplate` as the template

            ControlInfoJson.Item galleryTemplateChild = null;
            foreach (var child in control.Children)
            {
                if (child.Template.Name == _childTemplateName)
                {
                    galleryTemplateChild = child;
                    break;
                }
            }


            Contract.Assert(galleryTemplateChild != null);

            galleryTemplateChild.SkipWriteToSource = true;
            var controlRules = control.Rules.ToList();

            var defaulter = new DefaultRuleHelper(control, _galleryTemplate, _theme);
            foreach (var rule in galleryTemplateChild.Rules)
            {
                if (!defaulter.TryGetDefaultRule(rule.Property, out var defaultScript))
                    defaultScript = string.Empty;

                if (defaultScript == rule.InvariantScript)
                    continue;

                controlRules.Add(rule);
            }

            control.Rules = controlRules.ToArray();
            control.GalleryTemplateChildName = galleryTemplateChild.Name;
        }
    }
}
