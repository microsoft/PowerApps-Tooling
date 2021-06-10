// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal abstract class IRNode
    {
        /// <summary>
        /// Source Locations are only present when reading from source
        /// And should not be expected during the unpack operation
        /// </summary>
        public SourceLocation? SourceSpan;
        public abstract void Accept<Context>(IRNodeVisitor<Context> visitor, Context context);
    }

    /// <summary>
    /// Represents block construct, may have 0-N child blocks and 0-N properties
    /// </summary>
    [DebuggerDisplay("{Name}: {Properties.Count} props")]
    internal class BlockNode : IRNode, ICloneable<BlockNode>
    {
        public TypedNameNode Name;
        public IList<PropertyNode> Properties = new List<PropertyNode>();
        public IList<FunctionNode> Functions = new List<FunctionNode>();
        public IList<BlockNode> Children = new List<BlockNode>();

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }

        public BlockNode Clone()
        {
            return new BlockNode()
            {
                Name = Name.Clone(),
                Properties = Properties.Clone(),
                Functions = Functions.Clone(),
                Children = Children.Clone(),
            };
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
    internal class TypedNameNode : IRNode, ICloneable<TypedNameNode>
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

        public TypedNameNode Clone()
        {
            return new TypedNameNode()
            {
                Identifier = Identifier,
                Kind = Kind.Clone()
            };
        }
    }

    /// <summary>
    /// Represents a template like `label` or `gallery.HorizontalGallery`
    /// </summary>
    [DebuggerDisplay("{TypeName}.{OptionalVariant}")]
    internal class TypeNode : IRNode, ICloneable<TypeNode>
    {
        public string TypeName;
        public string OptionalVariant;

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }

        public TypeNode Clone()
        {
            return new TypeNode()
            {
                TypeName = TypeName,
                OptionalVariant = OptionalVariant
            };
        }
    }

    [DebuggerDisplay("{Identifier}: ={Expression}")]
    internal class PropertyNode : IRNode, ICloneable<PropertyNode>
    {
        public string Identifier;
        public ExpressionNode Expression;

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }

        public PropertyNode Clone()
        {
            return new PropertyNode()
            {
                Identifier = Identifier,
                Expression = Expression.Clone()
            };
        }
    }

    [DebuggerDisplay("{Identifier}({string.Join(',', Args)}):")]
    internal class FunctionNode : IRNode, ICloneable<FunctionNode>
    {
        public string Identifier;
        public IList<TypedNameNode> Args = new List<TypedNameNode>();
        public IList<ArgMetadataBlockNode> Metadata = new List<ArgMetadataBlockNode>();

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }

        public FunctionNode Clone()
        {
            return new FunctionNode()
            {
                Identifier = Identifier,
                Args = Args.Clone(),
                Metadata = Metadata.Clone(),
            };
        }
    }

    [DebuggerDisplay("{Identifier}: ={Default}")]
    internal class ArgMetadataBlockNode : IRNode, ICloneable<ArgMetadataBlockNode>
    {
        public string Identifier;
        public ExpressionNode Default;

        // In certain cases the Default value is not used as the InvariantScript value in the RuleEntry of the Parameter property.
        // Store this explicitly if that is the case to avoid modifying the app definition.
        public ExpressionNode InvariantScript { get; set; }

        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }

        public ArgMetadataBlockNode Clone()
        {
            return new ArgMetadataBlockNode()
            {
                Identifier = Identifier,
                Default = Default.Clone(),
            };
        }
    }

    [DebuggerDisplay("{Expression}")]
    internal class ExpressionNode : IRNode, ICloneable<ExpressionNode>
    {
        public string Expression;
        public override void Accept<Context>(IRNodeVisitor<Context> visitor, Context context)
        {
            visitor.Visit(this, context);
        }

        public ExpressionNode Clone()
        {
            return new ExpressionNode()
            {
                Expression = Expression
            };
        }
    }
}
