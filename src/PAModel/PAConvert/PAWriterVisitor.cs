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
    // Result is a bunch of strings, context is indentLevel
    internal class PAWriterVisitor : IRNodeVisitor<PAWriterVisitor.Context>
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

        public override void Visit(BlockNode node, Context context)
        {
            // Label1 as Label:
            context._sb.Clear();
            node.Name.Accept(this, context);
            context._yaml.WriteStartObject(context._sb.ToString());

            
            foreach (var func in node.Functions)
            {
                func.Accept(this, context);
            }

            foreach (var prop in node.Properties)
            {
                prop.Accept(this, context);
            }

            context._yaml.WriteNewline();

            foreach (var child in node.Children.OrderBy(child => GetZIndex(child)))
            {
                child.Accept(this, context);
            }

            context._yaml.WriteEndObject();            
        }

        // Use the ZIndex property of each control to order it with respect to it's parent
        // This matches the order shown in the tree view in studio
        private static double GetZIndex(BlockNode control)
        {
            if (control.Properties.Count == 0)
                return -1;
            var zindexProp = control.Properties.FirstOrDefault(prop => prop.Identifier == "ZIndex");
            if (zindexProp == default)
                return -1;
            if (!double.TryParse(zindexProp.Expression.Expression, out var zindexResult) || double.IsNaN(zindexResult) || double.IsInfinity(zindexResult))
                return -1;
            return zindexResult;
        }

        public override void Visit(TypedNameNode node, Context context)
        {
            context._sb.Append(CharacterUtils.EscapeName(node.Identifier));
            context._sb.Append(" As ");
            node.Kind.Accept(this, context);
        }

        public override void Visit(TypeNode node, Context context)
        {
            context._sb.Append(CharacterUtils.EscapeName(node.TypeName));

            if (!string.IsNullOrEmpty(node.OptionalVariant))
            {
                context._sb.Append(".");
                context._sb.Append(CharacterUtils.EscapeName(node.OptionalVariant));
            }            
        }

        public override void Visit(PropertyNode node, Context context)
        {
            context._sb.Clear();
            node.Expression.Accept(this, context);

            context._yaml.WriteProperty(CharacterUtils.EscapeName(node.Identifier), context._sb.ToString());
        }

        public override void Visit(FunctionNode node, Context context)
        {
            context._sb.Clear();
            context._sb.Append(CharacterUtils.EscapeName(node.Identifier)).Append('(');
            var isFirst = true;
            foreach (var arg in node.Args)
            {
                if (!isFirst)
                    context._sb.Append(", ");

                isFirst = false;
                arg.Accept(this, context);
            }
            context._sb.Append(")");
            var property = context._sb.ToString();

            context._sb.Clear();
            context._yaml.WriteStartObject(property);

            foreach (var metadataBlock in node.Metadata)
            {
                metadataBlock.Accept(this, context);
            }

            context._yaml.WriteEndObject();
        }

        public override void Visit(ExpressionNode node, Context context)
        {
            context._sb.Append(node.Expression);
        }

        public override void Visit(ArgMetadataBlockNode node, Context context)
        {
            context._yaml.WriteStartObject(node.Identifier);
            context._sb.Clear();
            node.Default.Accept(this, context);
            context._yaml.WriteProperty("Default", context._sb.ToString());
            context._yaml.WriteEndObject();
        }
    }
}
