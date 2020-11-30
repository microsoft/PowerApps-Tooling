// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public abstract void Accept<Context>(IRNodeVisitor<Context> visitor, Context context);
    }

    /// <summary>
    /// Represents block construct, may have 0-N child blocks and 0-N properties
    /// </summary>
    [DebuggerDisplay("{Name}: {Properties.Count} props")]
    internal class BlockNode : IRNode
    {
        public TypedNameNode Name;
        public IList<PropertyNode> Properties = new List<PropertyNode>();
        public IList<FunctionNode> Functions = new List<FunctionNode>();
        public IList<BlockNode> Children = new List<BlockNode>();

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
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
    [DebuggerDisplay("{Identifier} as {Kind}")]
    internal class TypedNameNode : IRNode
    {
        public string Identifier;

        /// <summary>
        /// Kind is not required in all cases. 
        /// </summary>
        public TypeNode Kind;

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }
    }

    /// <summary>
    /// Represents a template like `label` or `gallery.HorizontalGallery`
    /// </summary>
    [DebuggerDisplay("{TemplateName}.{OptionalVariant}")]
    internal class TypeNode : IRNode
    {
        public string TemplateName;
        public string OptionalVariant;

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }
    }

    [DebuggerDisplay("{Identifier}: ={Expression}")]
    internal class PropertyNode : IRNode
    {
        public string Identifier;
        public ExpressionNode Expression;

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }
    }

    internal class FunctionNode : IRNode
    {
        public string Identifier;
        public TypeNode ResultType;
        public IList<TypedNameNode> Args;
        public ExpressionNode Expression;

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }
    }

    [DebuggerDisplay("{Expression}")]
    internal class ExpressionNode : IRNode
    {
        public string Expression;
        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }
    }
}
