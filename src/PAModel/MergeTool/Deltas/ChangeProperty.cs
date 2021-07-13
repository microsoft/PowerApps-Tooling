// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class ChangeProperty : IDelta
    {
        public ControlPath ControlPath;
        public string PropertyName;
        public string Expression;
        public bool WasRemoved = false;

        public void Apply(CanvasDocument document)
        {
            var topParentName = ControlPath.Current;
            if (document._screens.TryGetValue(topParentName, out var blockNode))
            {
                ChangePropertyVisitor.ApplyChange(blockNode, ControlPath.Next(), PropertyName, Expression, WasRemoved);
                return;
            }

            if (document._components.TryGetValue(topParentName, out blockNode))
            {
                ChangePropertyVisitor.ApplyChange(blockNode, ControlPath.Next(), PropertyName, Expression, WasRemoved);
                return;
            }
            // Throw here? what does an error look like in this scenario
        }


        private class ChangePropertyVisitor : DefaultVisitor<ControlPath>
        {
            private string _property;
            private string _expression;
            private bool _wasRemoved;

            public static void ApplyChange(BlockNode block, ControlPath path, string property, string expression, bool wasRemoved)
            {
                new ChangePropertyVisitor(property, expression, wasRemoved).Visit(block, path);
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
                                return;
                            }

                            propertyNode.Accept(this, context);
                            return;
                        }
                    }

                    // Property wasn't present in base
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
            }
        }
    }
}
