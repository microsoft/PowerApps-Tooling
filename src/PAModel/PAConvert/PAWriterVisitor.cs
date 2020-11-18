// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    class Empty { }
    // Result is a bunch of strings, context is indentLevel
    internal class PAWriterVisitor : IRNodeVisitor<Empty, PAWriterVisitor.Context>
    {
        internal class Context
        {
            public YamlWriter _yaml;
            public StringBuilder _sb = new StringBuilder();
        }

        public PAWriterVisitor() { }

        public static string PrettyPrint(IRNode node)
        {
            StringWriter sw = new StringWriter();
            var yaml = new YamlWriter(sw);
            PAWriterVisitor pretty = new PAWriterVisitor();

            var context = new Context
            {
                _yaml = yaml
            };
            node.Accept(pretty, context);

            return sw.ToString();
        }

        public override Empty Visit(BlockNode node, Context context)
        {
            // Label1 as Label:
            context._sb.Clear();
            node.Name.Accept(this, context);
            context._yaml.WriteStartObject(context._sb.ToString());

            /* $$$
            foreach (var func in node.Functions)
            {
                result = result.With(func.Accept(this, childContext)).With("\n");
            }
            */

            foreach (var prop in node.Properties)
            {
                prop.Accept(this, context);
            }
                        

            foreach (var child in node.Children)
            {
                child.Accept(this, context);
            }

            context._yaml.WriteEndObject();

            return null;
        }

        public override Empty Visit(TypedNameNode node, Context context)
        {
            context._sb.Append(CharacterUtils.EscapeName(node.Identifier));
            context._sb.Append(" As ");
            node.Kind.Accept(this, context);

            return null;
        }

        public override Empty Visit(TemplateNode node, Context context)
        {
            context._sb.Append(CharacterUtils.EscapeName(node.TemplateName));

            if (!string.IsNullOrEmpty(node.OptionalVariant))
            {
                context._sb.Append(".");
                context._sb.Append(CharacterUtils.EscapeName(node.OptionalVariant));
            }
            return null;
        }

        public override Empty Visit(PropertyNode node, Context context)
        {
            context._sb.Clear();
            node.Expression.Accept(this, context);

            context._yaml.WriteProperty(CharacterUtils.EscapeName(node.Identifier), context._sb.ToString());
            return null;
        }

        public override Empty Visit(FunctionNode node, Context context)
        {
            return null;
        }

        public override Empty Visit(ExpressionNode node, Context context)
        {
            context._sb.Append(node.Expression);
            return null;
        }
    }
}
