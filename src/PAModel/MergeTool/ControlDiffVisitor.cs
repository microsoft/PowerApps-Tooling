// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool;

internal class ControlDiffVisitor : DefaultVisitor<ControlDiffContext>
{
    private List<IDelta> _deltas;
    private readonly EditorStateStore _childStateStore;
    private readonly TemplateStore _parentTemplateStore;
    private readonly TemplateStore _childTemplateStore;

    public static IEnumerable<IDelta> GetControlDelta(BlockNode ours, BlockNode parent, EditorStateStore childStateStore, TemplateStore parentTemplateStore, TemplateStore childTemplateStore, bool isInComponent)
    {
        var visitor = new ControlDiffVisitor(childStateStore, parentTemplateStore, childTemplateStore);
        visitor.Visit(ours, new ControlDiffContext(new ControlPath(new List<string>()), parent, isInComponent));

        return visitor._deltas;
    }

    private ControlDiffVisitor(EditorStateStore childStateStore, TemplateStore parentTemplateStore, TemplateStore childTemplateStore)
    {
        _deltas = new List<IDelta>();
        _childStateStore = childStateStore;
        _parentTemplateStore = parentTemplateStore;
        _childTemplateStore = childTemplateStore;
    }

    private Dictionary<string, ControlState> GetSubtreeStates(BlockNode node)
    {
        return GetSubtreeStatesImpl(node).ToDictionary(state => state.Name);
    }

    private IEnumerable<ControlState> GetSubtreeStatesImpl(BlockNode node)
    {
        var childstates = node.Children?.SelectMany(child => GetSubtreeStatesImpl(child)) ?? Enumerable.Empty<ControlState>();

        if (!_childStateStore.TryGetControlState(node.Name.Identifier, out var state))
            return childstates;

        return childstates.Concat(new List<ControlState>() { state });
    }

    public override void Visit(BlockNode node, ControlDiffContext context)
    {
        if (!(context.Theirs is BlockNode theirs))
            return;

        if (node.Name.Kind.TypeName != theirs.Name.Kind.TypeName)
        {
            _deltas.Add(new RemoveControl(context.Path, node.Name.Identifier, context.IsInComponent));
            _deltas.Add(new AddControl(context.Path, node, GetSubtreeStates(node), context.IsInComponent));

            return;
        }

        var controlPath = context.Path.Append(node.Name.Identifier);

        var theirChildrenDict = theirs.Children.ToDictionary(child => child.Name.Identifier);
        foreach (var child in node.Children)
        {
            var childName = child.Name.Identifier;
            if (theirChildrenDict.TryGetValue(childName, out var theirChild))
            {
                child.Accept(this, new ControlDiffContext(controlPath, theirChild, context.IsInComponent));
                theirChildrenDict.Remove(childName);
            }
            else
            {
                _deltas.Add(new AddControl(controlPath, child, GetSubtreeStates(child), context.IsInComponent));
            }
        }
        foreach (var kvp in theirChildrenDict)
        {
            _deltas.Add(new RemoveControl(controlPath, kvp.Key, context.IsInComponent));
        }

        _childTemplateStore.TryGetTemplate(node.Name.Identifier, out var childTemplate);
        // Let's handle properties:
        var theirPropDict = theirs.Properties.ToDictionary(prop => prop.Identifier);
        foreach (var prop in node.Properties)
        {
            var propName = prop.Identifier;
            if (theirPropDict.TryGetValue(propName, out var theirProp))
            {
                prop.Accept(this, new ControlDiffContext(controlPath, theirProp, context.IsInComponent));
                theirPropDict.Remove(propName);
            }
            else
            {
                // Added property
                if (childTemplate?.IsComponentTemplate ?? false)
                {
                    var childCustomProperties = childTemplate.CustomProperties.ToDictionary(c => c.Name);
                    if (childCustomProperties.TryGetValue(prop.Identifier, out var customProp))
                    {
                        _deltas.Add(ChangeProperty.GetComponentCustomPropertyChange(controlPath, prop.Identifier, prop.Expression.Expression, customProp));
                        continue;
                    }
                }
                _deltas.Add(ChangeProperty.GetNormalPropertyChange(controlPath, prop.Identifier, prop.Expression.Expression));
            }
        }

        // Removed props
        foreach (var kvp in theirPropDict)
        {
            _deltas.Add(ChangeProperty.GetPropertyRemovedChange(controlPath, kvp.Key));
        }


        // And component functions
        var theirComponentFunctions = theirs.Functions.ToDictionary(func => func.Identifier);
        foreach (var func in node.Functions)
        {
            var propName = func.Identifier;
            if (theirComponentFunctions.TryGetValue(propName, out var theirFunc))
            {
                func.Accept(this, new ControlDiffContext(controlPath, theirFunc, context.IsInComponent));
                theirComponentFunctions.Remove(propName);
            }
            else
            {
                if (childTemplate?.IsComponentTemplate ?? false)
                {
                    var childCustomProperties = childTemplate.CustomProperties.ToDictionary(c => c.Name);
                    if (childCustomProperties.TryGetValue(func.Identifier, out var customProperty))
                    {
                        _deltas.Add(ChangeComponentFunction.GetFunctionChangeWithMetadata(context.Path, func.Identifier, func, customProperty));
                        continue;
                    }
                }
            }
        }

        // Removed functions
        foreach (var kvp in theirComponentFunctions)
        {
            _deltas.Add(ChangeComponentFunction.GetFunctionRemoval(controlPath, kvp.Key));
        }

    }

    public override void Visit(PropertyNode node, ControlDiffContext context)
    {
        if (!(context.Theirs is PropertyNode theirs))
            return;

        var currentControlKind = context.Path.Current;
        if (TryGetUpdatedCustomPropertyMetadata(currentControlKind, node.Identifier, out var customProperty))
        {
            _deltas.Add(ChangeProperty.GetComponentCustomPropertyChange(context.Path, node.Identifier, node.Expression.Expression, customProperty));
            return;
        }

        if (node.Expression.Expression != theirs.Expression.Expression)
            _deltas.Add(ChangeProperty.GetNormalPropertyChange(context.Path, node.Identifier, node.Expression.Expression));
    }

    public override void Visit(FunctionNode node, ControlDiffContext context)
    {
        if (!(context.Theirs is FunctionNode theirs))
            return;

        var currentControlKind = context.Path.Current;
        if (TryGetUpdatedCustomPropertyMetadata(currentControlKind, node.Identifier, out var customProperty))
        {
            _deltas.Add(ChangeComponentFunction.GetFunctionChangeWithMetadata(context.Path, node.Identifier, node, customProperty));
            return;
        }

        if (!node.Equals(theirs))
        {
            _deltas.Add(ChangeComponentFunction.GetFunctionChange(context.Path, node.Identifier, node));
        }
    }

    private bool TryGetUpdatedCustomPropertyMetadata(string currentControlKind, string propertyName, out Schemas.CustomPropertyJson customProperty)
    {
        if (_childTemplateStore.TryGetTemplate(currentControlKind, out var template) &&
            (template.IsComponentTemplate ?? false) &&
            _parentTemplateStore.TryGetTemplate(currentControlKind, out var parentTemplate))
        {
            var childCustomProperties = template.CustomProperties.ToDictionary(c => c.Name);
            var parentCustomProperties = parentTemplate.CustomProperties.ToDictionary(c => c.Name);
            if (childCustomProperties.TryGetValue(propertyName, out customProperty))
            {
                return !parentCustomProperties.TryGetValue(propertyName, out var parentCustomProp) ||
                    parentCustomProp != customProperty;
            }
        }
        customProperty = null;
        return false;
    }
}

internal class ControlDiffContext
{
    public IRNode Theirs { get; }
    public ControlPath Path { get; }
    public bool IsInComponent { get; }

    public ControlDiffContext(ControlPath path, IRNode theirs, bool isInComponent)
    {
        Theirs = theirs;
        Path = path;
        IsInComponent = isInComponent;
    }
}
