// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal abstract class IRNodeVisitor<Context>
    {
        public abstract void Visit(BlockNode node, Context context);
        public abstract void Visit(TypedNameNode node, Context context);
        public abstract void Visit(TypeNode node, Context context);
        public abstract void Visit(PropertyNode node, Context context);
        public abstract void Visit(FunctionNode node, Context context);
        public abstract void Visit(ExpressionNode node, Context context);
    }
}
