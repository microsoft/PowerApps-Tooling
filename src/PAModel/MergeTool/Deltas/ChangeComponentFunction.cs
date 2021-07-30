// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class ChangeComponentFunction : IDelta
    {
        private ControlPath _controlPath;
        private string _propertyName;
        private FunctionNode _func;
        private bool _wasRemoved;

        // For a function that changed
        public ChangeComponentFunction(ControlPath path, string propertyName, FunctionNode func)
        {
            _controlPath = path;
            _propertyName = propertyName;
            _func = func;
            _wasRemoved = false;
        }

        // For a function that was removed
        public ChangeComponentFunction(ControlPath path, string propertyName)
        {
            _controlPath = path;
            _propertyName = propertyName;
            _wasRemoved = true;
        }

        public void Apply(CanvasDocument document)
        {
            var topParentName = _controlPath.Current;
            if (document._screens.TryGetValue(topParentName, out var blockNode))
            {
                ChangeComponentFunctionVisitor.ApplyChange(blockNode, _controlPath.Next(), _propertyName, _func, _wasRemoved);
                return;
            }

            if (document._components.TryGetValue(topParentName, out blockNode))
            {
                ChangeComponentFunctionVisitor.ApplyChange(blockNode, _controlPath.Next(), _propertyName, _func, _wasRemoved);
                return;
            }
        }


        private class ChangeComponentFunctionVisitor : DefaultVisitor<ControlPath>
        {
            private string _property;
            private FunctionNode _func;
            private bool _wasRemoved;

            public static void ApplyChange(BlockNode block, ControlPath path, string property, FunctionNode func, bool wasRemoved)
            {
                new ChangeComponentFunctionVisitor(property, func, wasRemoved).Visit(block, path);
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

                            if (_wasRemoved)
                                return;

                            node.Functions.Add(_func);
                        }
                    }

                    // Function wasn't present in base
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
}
