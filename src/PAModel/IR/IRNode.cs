// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    internal class BlockNode : IRNode, ICloneable<BlockNode>, IEquatable<BlockNode>
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

        public bool Equals(BlockNode other)
        {
            return other != null &&
                Name == other.Name &&
                Properties.SequenceEqual(other.Properties) &&
                Functions.SequenceEqual(other.Functions) &&
                Children.SequenceEqual(other.Children);
        }

        public override bool Equals(object obj)
        {
            return obj is BlockNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (SourceSpan, Name, Properties, Functions, Children).GetHashCode();
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
    internal class TypedNameNode : IRNode, ICloneable<TypedNameNode>, IEquatable<TypedNameNode>
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

        public override bool Equals(object obj)
        {
            return obj is TypedNameNode other && Equals(other);
        }

        public bool Equals(TypedNameNode other)
        {
            return other != null &&
                Identifier == other.Identifier &&
                Kind == other.Kind;
        }

        public override int GetHashCode()
        {
            return (SourceSpan, Identifier, Kind).GetHashCode();
        }
    }

    /// <summary>
    /// Represents a template like `label` or `gallery.HorizontalGallery`
    /// </summary>
    [DebuggerDisplay("{TypeName}.{OptionalVariant}")]
    internal class TypeNode : IRNode, ICloneable<TypeNode>, IEquatable<TypeNode>
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

        public override bool Equals(object obj)
        {
            return obj is TypeNode other && Equals(other);
        }

        public bool Equals(TypeNode other)
        {
            return other != null &&
                TypeName == other.TypeName &&
                OptionalVariant == other.OptionalVariant;
        }

        public override int GetHashCode()
        {
            return (SourceSpan, TypeName, OptionalVariant).GetHashCode();
        }
    }

    [DebuggerDisplay("{Identifier}: ={Expression}")]
    internal class PropertyNode : IRNode, ICloneable<PropertyNode>, IEquatable<PropertyNode>
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

        public override bool Equals(object obj)
        {
            return obj is PropertyNode other && Equals(other);
        }

        public bool Equals(PropertyNode other)
        {
            return other != null &&
                Identifier == other.Identifier &&
                Expression == other.Expression;
        }

        public override int GetHashCode()
        {
            return (SourceSpan, Identifier, Expression).GetHashCode();
        }
    }

    [DebuggerDisplay("{Identifier}({string.Join(',', Args)}):")]
    internal class FunctionNode : IRNode, ICloneable<FunctionNode>, IEquatable<FunctionNode>
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

        public override bool Equals(object obj)
        {
            return obj is FunctionNode other && Equals(other);
        }

        public bool Equals(FunctionNode other)
        {
            return other != null &&
                Identifier == other.Identifier &&
                Args.SequenceEqual(other.Args) &&
                Metadata.SequenceEqual(other.Metadata);
        }

        public override int GetHashCode()
        {
            return (SourceSpan, Identifier, Args, Metadata).GetHashCode();
        }
    }

    [DebuggerDisplay("{Identifier}: ={Default}")]
    internal class ArgMetadataBlockNode : IRNode, ICloneable<ArgMetadataBlockNode>, IEquatable<ArgMetadataBlockNode>
    {
        public string Identifier;
        public ExpressionNode Default;

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

        public override bool Equals(object obj)
        {
            return obj is ArgMetadataBlockNode other && Equals(other);
        }

        public bool Equals(ArgMetadataBlockNode other)
        {
            return other != null &&
                Identifier == other.Identifier &&
                Default == other.Default;
        }

        public override int GetHashCode()
        {
            return (SourceSpan, Identifier, Default).GetHashCode();
        }
    }

    [DebuggerDisplay("{Expression}")]
    internal class ExpressionNode : IRNode, ICloneable<ExpressionNode>, IEquatable<ExpressionNode>
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

        public override bool Equals(object obj)
        {
            return obj is ExpressionNode other && Equals(other);
        }

        public bool Equals(ExpressionNode other)
        {
            return other != null &&
                Expression == other.Expression;
        }

        public override int GetHashCode()
        {
            return (SourceSpan, Expression).GetHashCode();
        }
    }
}
