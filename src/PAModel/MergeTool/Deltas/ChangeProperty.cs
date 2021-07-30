// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class ChangeProperty : IDelta
    {
        public readonly ControlPath ControlPath;
        public readonly string PropertyName;
        private readonly string _expression;
        private readonly bool _wasRemoved;
        private readonly CustomPropertyJson _customProperty;

        // For a normal property that changed
        public ChangeProperty(ControlPath path, string propertyName, string expression)
        {
            ControlPath = path;
            PropertyName = propertyName;
            _expression = expression;
            _wasRemoved = false;
        }

        // For a component custom property that changed
        public ChangeProperty(ControlPath path, string propertyName, string expression, CustomPropertyJson customProperty)
        {
            ControlPath = path;
            PropertyName = propertyName;
            _expression = expression;
            _customProperty = customProperty;
            _wasRemoved = false;
        }

        // For a property that was removed
        public ChangeProperty(ControlPath path, string propertyName)
        {
            ControlPath = path;
            PropertyName = propertyName;
            _wasRemoved = true;
        }

        public void Apply(CanvasDocument document)
        {
            var topParentName = ControlPath.Current;
            if (document._screens.TryGetValue(topParentName, out var blockNode))
            {
                if (!ChangePropertyVisitor.ApplyChange(blockNode, ControlPath.Next(), PropertyName, _expression, _wasRemoved))
                    return;
            }

            if (document._components.TryGetValue(topParentName, out blockNode))
            {
                if (!ChangePropertyVisitor.ApplyChange(blockNode, ControlPath.Next(), PropertyName, _expression, _wasRemoved))
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


        private class ChangePropertyVisitor : DefaultVisitor<ControlPath>
        {
            private string _property;
            private string _expression;
            private bool _wasRemoved;
            private bool _success = false;

            public static bool ApplyChange(BlockNode block, ControlPath path, string property, string expression, bool wasRemoved)
            {
                var visitor = new ChangePropertyVisitor(property, expression, wasRemoved);
                visitor.Visit(block, path);
                return visitor._success;
            }

            private ChangePropertyVisitor(string property, string expression, bool wasRemoved)
            {
                _property = property;
                _expression = expression;
                _wasRemoved = wasRemoved;
            }

            public override void Visit(BlockNode node, ControlPath context)
            {
                var searchingControlName = context.Current;

                // Found the control, look for the property
                if (searchingControlName == null)
                {
                    foreach (var propertyNode in node.Properties)
                    {
                        if (propertyNode.Identifier == _property)
                        {
                            if (_wasRemoved)
                            {
                                node.Properties.Remove(propertyNode);
                                _success = true;
                                return;
                            }

                            propertyNode.Accept(this, context);
                            return;
                        }
                    }

                    // Property wasn't present in base
                    _success = true;
                    node.Properties.Add(new PropertyNode() { Expression = new ExpressionNode() { Expression = _expression }, Identifier = _property });
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

            public override void Visit(PropertyNode node, ControlPath context)
            {
                if (node.Identifier != _property)
                    return;

                node.Expression.Accept(this, context);
            }

            public override void Visit(ExpressionNode node, ControlPath context)
            {
                node.Expression = _expression;
                _success = true;
            }
        }
    }
}
