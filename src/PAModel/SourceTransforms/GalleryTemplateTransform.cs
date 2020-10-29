// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
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
        private EditorStateStore _controlStore;

        public GalleryTemplateTransform(Dictionary<string, ControlTemplate> templateStore, EditorStateStore stateStore)
        {
            templateStore.TryGetValue(_childTemplateName, out var template);
            _galleryTemplate = template;
            _galleryTemplateJson = ControlInfoJson.Template.CreateDefaultTemplate(_childTemplateName, _galleryTemplate);
            _controlStore = stateStore;
        }

        public void AfterParse(BlockNode control)
        {
            // This will only be called on a control with the Gallery template.
            // If .studiostate was present, there will be a key for the child galleryTemplate;
            var controlName = control.Name.Identifier;
            string galleryTemplateName = null;
            if (_controlStore.TryGetControlState(controlName, out var galleryState))
                galleryTemplateName = galleryState.GalleryTemplateChildName;

            if (galleryTemplateName == null)
            {
                // create unambiguous name for gallery template control
                var index = 1;
                while (_controlStore.TryGetControlState(control.Name + "template" + index, out _))
                    index++;

                galleryTemplateName = control.Name + "template" + index;
            }

            var parentCombinedRules = control.Properties.ToDictionary(prop => prop.Identifier);
            var childRules = new List<PropertyNode>();
            foreach (var rule in parentCombinedRules)
            {
                if (_galleryTemplate.InputDefaults.ContainsKey(rule.Key))
                {
                    childRules.Add(rule.Value);
                    control.Properties.Remove(rule.Value);
                }
            }

            var galleryTemplateChild = new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = galleryTemplateName,
                    Kind = new TemplateNode()
                    {
                        TemplateName = _childTemplateName
                    }
                },
                Children = new List<BlockNode>(),
                Functions = new List<FunctionNode>(),
                Properties = childRules
            };



            control.Children = control.Children.Prepend(galleryTemplateChild).ToList();
        }

        public void BeforeWrite(BlockNode control)
        {
            BlockNode galleryTemplateChild = null;
            foreach (var child in control.Children)
            {
                if (child.Name.Kind.TemplateName == _childTemplateName)
                {
                    galleryTemplateChild = child;
                    break;
                }
            }


            Contract.Assert(galleryTemplateChild != null);

            _controlStore.Remove(galleryTemplateChild.Name.Identifier);
            control.Properties = control.Properties.Concat(galleryTemplateChild.Properties).ToList();
            control.Children.Remove(galleryTemplateChild);
        }
    }
}
