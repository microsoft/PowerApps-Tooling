// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class ChangeComponentFunction : IDelta
{
    public readonly ControlPath ControlPath;
    public readonly string PropertyName;
    private readonly FunctionNode _func;
    private readonly bool _wasRemoved;
    private readonly CustomPropertyJson _customProperty;

    private ChangeComponentFunction(ControlPath path, string propertyName, FunctionNode func, CustomPropertyJson customProperty, bool wasRemoved)
    {
        ControlPath = path;
        PropertyName = propertyName;
        _func = func;
        _customProperty = customProperty;
        _wasRemoved = wasRemoved;
    }

    public static ChangeComponentFunction GetFunctionChangeWithMetadata(ControlPath path, string propertyName, FunctionNode func, CustomPropertyJson customProperty)
    {
        return new ChangeComponentFunction(path, propertyName, func, customProperty, false);
    }

    public static ChangeComponentFunction GetFunctionChange(ControlPath path, string propertyName, FunctionNode func)
    {
        return new ChangeComponentFunction(path, propertyName, func, null, false);
    }

    public static ChangeComponentFunction GetFunctionRemoval(ControlPath path, string propertyName)
    {
        return new ChangeComponentFunction(path, propertyName, null, null, true);
    }

    public void Apply(CanvasDocument document)
    {
        var topParentName = ControlPath.Current;
        if (document._screens.TryGetValue(topParentName, out var blockNode))
        {
            if (!ChangeComponentFunctionVisitor.ApplyChange(blockNode, ControlPath.Next(), PropertyName, _func, _wasRemoved))
                return;
        }

        if (document._components.TryGetValue(topParentName, out blockNode))
        {
            if (!ChangeComponentFunctionVisitor.ApplyChange(blockNode, ControlPath.Next(), PropertyName, _func, _wasRemoved))
                return;

            if (document._templateStore.TryGetTemplate(topParentName, out var updatableTemplate))
            {
                var customProps = updatableTemplate.CustomProperties.ToDictionary(prop => prop.Name);
                if (_wasRemoved)
                    customProps.Remove(PropertyName);
                else if (_customProperty != null)
                    customProps[PropertyName] = _customProperty;
                else
                    return;
                updatableTemplate.CustomProperties = customProps.Values.ToArray();
            }
        }
    }


    private class ChangeComponentFunctionVisitor : DefaultVisitor<ControlPath>
    {
        private readonly string _property;
        private readonly FunctionNode _func;
        private readonly bool _wasRemoved;
        private bool _success;

        public static bool ApplyChange(BlockNode block, ControlPath path, string property, FunctionNode func, bool wasRemoved)
        {
            var visitor = new ChangeComponentFunctionVisitor(property, func, wasRemoved);
            visitor.Visit(block, path);
            return visitor._success;
        }

        private ChangeComponentFunctionVisitor(string property, FunctionNode func, bool wasRemoved)
        {
            _property = property;
            _func = func;
            _wasRemoved = wasRemoved;
        }

        public override void Visit(BlockNode node, ControlPath context)
        {
            var searchingControlName = context.Current;

            // Found the control, look for the Function
            if (searchingControlName == null)
            {
                foreach (var funcNode in node.Functions)
                {
                    if (funcNode.Identifier == _property)
                    {
                        node.Functions.Remove(funcNode);
                        _success = true;

                        if (_wasRemoved)
                            return;

                        node.Functions.Add(_func);
                    }
                }

                // Function wasn't present in base
                _success = true;
                node.Functions.Add(_func);
                return;
            }

            foreach (var childBlock in node.Children)
            {
                if (childBlock.Name.Identifier == searchingControlName)
                {
                    childBlock.Accept(this, context.Next());
                    return;
                }
            }
        }
    }
}
