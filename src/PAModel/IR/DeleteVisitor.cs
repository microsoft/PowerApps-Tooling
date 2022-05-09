// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal class DeleteVisitor : DefaultVisitor<DeleteVisitorContext>
    {
        public ErrorContainer Errors { get; } = new ErrorContainer();
        private Action<BlockNode> CreateDeleteNode(BlockNode node)
        {
            return nodeToDelete =>
            {
                if (node.Children.Contains(nodeToDelete))
                {
                    node.Children.Remove(nodeToDelete);
                }
                else
                {
                    Errors.AddError(ErrorCode.Generic, SourceLocation.FromFile(""), $"Node {nodeToDelete.Name.Identifier} located but could not be deleted.");
                }
            };
        }

        public override void Visit(BlockNode node, DeleteVisitorContext context)
        {
            if (node.Name.Identifier == context.NameToDelete)
            {
                context.DeleteNode(node);
                context.DidDelete = true;
                return;
            }

            foreach (var child in node.Children)
            {
                context.DeleteNode = CreateDeleteNode(node);
                child.Accept(this, context);
                if (context.DidDelete)
                    return;
            }
        }
    }
}
