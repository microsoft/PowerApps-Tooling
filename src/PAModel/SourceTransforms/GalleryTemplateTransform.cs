// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;

internal class GalleryTemplateTransform : IControlTemplateTransform
{
    private static readonly IEnumerable<string> _targets = new List<string>() { "gallery" };
    public IEnumerable<string> TargetTemplates => _targets;

    private const string _childTemplateName = "galleryTemplate";

    private readonly ControlTemplate _galleryTemplate;
    private readonly EditorStateStore _controlStore;

    public GalleryTemplateTransform(Dictionary<string, ControlTemplate> defaultValueTemplates, EditorStateStore stateStore)
    {
        defaultValueTemplates.TryGetValue(_childTemplateName, out var template);
        _galleryTemplate = template;
        _controlStore = stateStore;
    }

    public void BeforeWrite(BlockNode control)
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
            while (_controlStore.TryGetControlState(control.Name.Identifier + "template" + index, out _))
                index++;

            galleryTemplateName = control.Name.Identifier + "template" + index;
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
                Kind = new TypeNode()
                {
                    TypeName = _childTemplateName
                }
            },
            Children = new List<BlockNode>(),
            Functions = new List<FunctionNode>(),
            Properties = childRules
        };

        control.Children = control.Children.Prepend(galleryTemplateChild).ToList();
    }

    public void AfterRead(BlockNode control)
    {
        BlockNode galleryTemplateChild = null;

        foreach (var child in control.Children)
        {
            if (child.Name.Kind.TypeName == _childTemplateName)
            {
                galleryTemplateChild = child;
                break;
            }
        }

        if (galleryTemplateChild == null)
        {
            return;
        }

        control.Properties = control.Properties.Concat(galleryTemplateChild.Properties).ToList();
        control.Children.Remove(galleryTemplateChild);

        if (_controlStore.TryGetControlState(control.Name.Identifier, out var galleryState))
        {
            galleryState.GalleryTemplateChildName = galleryTemplateChild.Name.Identifier;
        }
    }
}
