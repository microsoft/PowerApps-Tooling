using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal abstract class IRNode
    {
        /// <summary>
        /// Source Locations are only present when reading from source
        /// And should not be expected during the unpack operation
        /// </summary>
        public readonly SourceLocation? SourceSpan;
        public abstract Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context);
    }

    /// <summary>
    /// Represents block construct, may have 0-N child blocks and 0-N properties
    /// </summary>
    internal class BlockNode : IRNode
    {
        public TypedNameNode Name;
        public IList<PropertyNode> Properties = new List<PropertyNode>();
        public IList<FunctionNode> Functions = new List<FunctionNode>();
        public IList<BlockNode> Children = new List<BlockNode>();

        public override Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }

    /// <summary>
    /// Represents names with types like
    /// lblTest As label:
    ///
    /// where
    /// Identifier = lblTest
    /// Kind = label
    /// </summary>
    internal class TypedNameNode : IRNode
    {
        public string Identifier;

        /// <summary>
        /// Kind is not required in all cases. 
        /// </summary>
        public TemplateNode Kind;

        public override Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }

    /// <summary>
    /// Represents a template like `label` or `gallery.HorizontalGallery`
    /// </summary>
    internal class TemplateNode : IRNode
    {
        public string TemplateName;
        public string OptionalVariant;

        public override Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }


    internal class PropertyNode : IRNode
    {
        public string Identifier;
        public ExpressionNode Expression;

        public override Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }

    internal class FunctionNode : IRNode
    {
        public string Identifier;
        public IList<TypedNameNode> Args;
        public ExpressionNode Expression;

        public override Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }

    internal class ExpressionNode : IRNode
    {
        public string Expression;
        public override Result Accept<Result, Context>(IRNodeVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }
}
