// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Result is a bunch of strings, context is indentLevel
    internal class PAWriterVisitor : IRNodeVisitor<LazyList<string>, PAWriterVisitor.Context>
    {
        public PAWriterVisitor() { }

        public static string PrettyPrint(IRNode node)
        {
            PAWriterVisitor pretty = new PAWriterVisitor();
            return string.Concat(PAConstants.Header, "\n", string.Concat(node.Accept(pretty, new Context(1))));
        }

        public override LazyList<string> Visit(BlockNode node, Context context)
        {
            var result = LazyList<string>.Of(context.GetNewLine());

            result = result.With(node.Name.Accept(this, context));

            var childContext = context.Indent();
            foreach (var func in node.Functions)
            {
                result = result.With(func.Accept(this, childContext)).With("\n");
            }

            foreach (var prop in node.Properties)
            {
                result = result.With(prop.Accept(this, childContext));
            }

            if (node.Properties.Any())
                result = result.With("\n");

            foreach (var child in node.Children)
            {
                result = result.With(child.Accept(this, childContext));
            }

            return result;
        }

        public override LazyList<string> Visit(TypedNameNode node, Context context)
        {
            return LazyList<string>.Of(PAConstants.ControlKeyword, " ", node.Identifier, " : ").With(node.Kind.Accept(this, context));
        }

        public override LazyList<string> Visit(TemplateNode node, Context context)
        {
            var result = LazyList<string>.Of(node.TemplateName);
            if (!string.IsNullOrEmpty(node.OptionalVariant))
                result = result.With(", ", node.OptionalVariant);
            return result;
        }

        public override LazyList<string> Visit(PropertyNode node, Context context)
        {
            var result = LazyList<string>.Of(context.GetNewLine());
            return result.With(node.Identifier, " =").With(node.Expression.Accept(this, context.Indent()));
        }

        public override LazyList<string> Visit(FunctionNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override LazyList<string> Visit(ExpressionNode node, Context context)
        {
            var isMultiline = node.Expression.Contains("\n");
            if (!isMultiline)
                return LazyList<string>.Of(" ", node.Expression);
            var result = LazyList<string>.Empty;
            foreach (var line in node.Expression.Split('\n'))
            {
                result = result
                    .With(context.GetNewLine())
                    .With(line.TrimEnd('\n'));
            }
            return result;
        }

        internal class Context
        {
            public int IndentDepth { get; }

            public Context(int indentDepth)
            {
                IndentDepth = indentDepth;
            }

            internal Context Indent()
            {
                return new Context(indentDepth: IndentDepth + 1);
            }

            internal string GetNewLine()
            {
                return "\n" + GetNewLineIndent(IndentDepth);
            }
            internal string GetNewLineIndent(int indentation)
            {
                return new string(' ', 4*(indentation - 1));
            }

        }

    }
}
