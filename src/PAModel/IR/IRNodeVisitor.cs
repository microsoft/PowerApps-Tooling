using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal abstract class IRNodeVisitor<Result, Context>
    {
        public abstract Result Visit(BlockNode node, Context context);
        public abstract Result Visit(TypedNameNode node, Context context);
        public abstract Result Visit(TemplateNode node, Context context);
        public abstract Result Visit(PropertyNode node, Context context);
        public abstract Result Visit(FunctionNode node, Context context);
        public abstract Result Visit(ExpressionNode node, Context context);
    }
}
