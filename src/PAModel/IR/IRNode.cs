using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.AST
{
    internal class IRNode
    {
        /// <summary>
        /// Source Locations are only present when reading from source
        /// And should not be expected during the unpack operation
        /// </summary>
        public readonly SourceLocation? SourceSpan;
    }

    /// <summary>
    /// Represents block construct, may have 0-N child blocks and 0-N properties
    /// </summary>
    internal class BlockNode : IRNode
    {
        public TypedNameNode Name;
        public IList<PropertyNode> Properties;
        public IList<FunctionNode> Functions;
        public IList<BlockNode> Children;
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
    }

    /// <summary>
    /// Represents a template like `label` or `gallery.HorizontalGallery`
    /// </summary>
    internal class TemplateNode : IRNode
    {
        public string TemplateName;
        public string OptionalVariant;
    }


    internal class PropertyNode : IRNode
    {
        public string Identifier;
        public ExpressionNode Expression;
    }

    internal class FunctionNode : IRNode
    {
        public string Identifier;
        public IList<TypedNameNode> Args;
        public ExpressionNode Expression;
    }

    internal class ExpressionNode : IRNode
    {
        public string Expression;
    }
}
