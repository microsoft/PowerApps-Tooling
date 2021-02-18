using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool
{
    internal class ControlDiffVisitor : IRNodeVisitor<ControlDiffContext>
    {
        private List<IDelta> _deltas;

        public static IEnumerable<IDelta> GetControlDelta(BlockNode ours, BlockNode parent)
        {
            var visitor = new ControlDiffVisitor();
            visitor.Visit(ours, new ControlDiffContext() { Theirs = parent, Path = new ControlPath(new List<string>()) });

            return visitor._deltas;
        }

        private ControlDiffVisitor()
        {
            _deltas = new List<IDelta>();
        }

        public override void Visit(BlockNode node, ControlDiffContext context)
        {
            if (!(context.Theirs is BlockNode theirs))
                return;
            if (node.Name.Kind.TypeName != theirs.Name.Kind.TypeName)
                // add and remove diff
                return;

            var controlPath = context.Path.Append(node.Name.Identifier);

            var theirChildrenDict = theirs.Children.ToDictionary(child => child.Name.Identifier);
            foreach (var child in node.Children)
            {
                var childName = child.Name.Identifier;
                if (theirChildrenDict.TryGetValue(childName, out var theirChild))
                {
                    child.Accept(this, new ControlDiffContext() { Path = controlPath, Theirs = theirChild });
                    theirChildrenDict.Remove(childName);
                }
                else
                {
                    // Added control
                }
            }
            foreach (var kvp in theirChildrenDict)
            {
                // removed control
            }

            // Let's handle properties:
            var theirPropDict = theirs.Properties.ToDictionary(prop => prop.Identifier);
            foreach (var prop in node.Properties)
            {
                var propName = prop.Identifier;
                if (theirPropDict.TryGetValue(propName, out var theirProp))
                {
                    prop.Accept(this, new ControlDiffContext() { Path = controlPath, Theirs = theirProp });
                    theirPropDict.Remove(propName);
                }
                else
                {
                    // Added prop
                    _deltas.Add(new ChangeProperty() { ControlPath = controlPath, Expression = prop.Expression.Expression, PropertyName = prop.Identifier });
                }
            }

            foreach (var kvp in theirChildrenDict)
            {
                // removed prop
                _deltas.Add(new ChangeProperty() { ControlPath = controlPath, WasRemoved = true });
            }


        }

        public override void Visit(PropertyNode node, ControlDiffContext context)
        {
            if (!(context.Theirs is PropertyNode theirs))
                return;

            // Maybe add smarter diff here eventually
            if (node.Expression.Expression != theirs.Expression.Expression)
                _deltas.Add(new ChangeProperty() { ControlPath = context.Path, Expression = node.Expression.Expression, PropertyName = node.Identifier });
        }


        /// Ignore below here (refactor)
        /// 
        public override void Visit(ExpressionNode node, ControlDiffContext context)
        {
            throw new NotImplementedException();
        }

        public override void Visit(TypedNameNode node, ControlDiffContext context)
        {
            throw new NotImplementedException();
        }

        public override void Visit(TypeNode node, ControlDiffContext context)
        {
            throw new NotImplementedException();
        }

        public override void Visit(ArgMetadataBlockNode node, ControlDiffContext context)
        {
            throw new NotImplementedException();
        }
        public override void Visit(FunctionNode node, ControlDiffContext context)
        {
            throw new NotImplementedException();
        }
    }

    internal class ControlDiffContext
    {
        public IRNode Theirs;
        public ControlPath Path;
    }
}
