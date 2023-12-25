// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.IR;

internal abstract class IRNodeVisitor<Context>
{
    public abstract void Visit(BlockNode node, Context context);
    public abstract void Visit(TypedNameNode node, Context context);
    public abstract void Visit(TypeNode node, Context context);
    public abstract void Visit(PropertyNode node, Context context);
    public abstract void Visit(FunctionNode node, Context context);
    public abstract void Visit(ExpressionNode node, Context context);
    public abstract void Visit(ArgMetadataBlockNode node, Context context);
}

internal class DefaultVisitor<Context> : IRNodeVisitor<Context>
{
    public override void Visit(BlockNode node, Context context)
    {
        throw new NotImplementedException();
    }

    public override void Visit(TypedNameNode node, Context context)
    {
        throw new NotImplementedException();
    }

    public override void Visit(TypeNode node, Context context)
    {
        throw new NotImplementedException();
    }

    public override void Visit(PropertyNode node, Context context)
    {
        throw new NotImplementedException();
    }

    public override void Visit(FunctionNode node, Context context)
    {
        throw new NotImplementedException();
    }

    public override void Visit(ExpressionNode node, Context context)
    {
        throw new NotImplementedException();
    }

    public override void Visit(ArgMetadataBlockNode node, Context context)
    {
        throw new NotImplementedException();
    }
}
