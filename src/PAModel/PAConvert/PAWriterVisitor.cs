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
            return string.Concat(node.Accept(pretty, new Context(0)));
        }

        public override LazyList<string> Visit(BlockNode node, Context context)
        {
            var result = LazyList<string>.Of(context.GetNewLine());

            result = result.With(node.Name.Accept(this, context));

            var childContext = context.Indent();
            foreach (var func in node.Functions)
            {
                result = result.With(func.Accept(this, childContext));
            }

            foreach (var prop in node.Properties)
            {
                result = result.With(prop.Accept(this, childContext));
            }

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



        //private void NewLine()
        //{
        //    _sb.AppendLine(IndentString);
        //}

        //private void Append(string content)
        //{
        //    _sb.Append(content);
        //}

        //public void WriteBlock(BlockNode control)
        //{
        //    var controlTemplate = CharacterUtils.EscapeName(control.Template.Name);
        //    if (control.VariantName.Length > 0)
        //        controlTemplate += $"{PAConstants.ControlVariantSeparator} {CharacterUtils.EscapeName(control.VariantName)}";

        //    WriteLine($"{(isComponent ? PAConstants.ComponentKeyword : PAConstants.ControlKeyword)} {CharacterUtils.EscapeName(control.Name)} {PAConstants.ControlTemplateSeparator} {controlTemplate}");
        //    _indentLevel++;

        //    _templates.TryGetValue(control.Template.Name, out var template);

        //    var defaulter = new DefaultRuleHelper(control, template, _theme);

        //    foreach (var rule in control.Rules)
        //    {
        //        if (!defaulter.TryGetDefaultRule(rule.Property, out var defaultScript))
        //            defaultScript = string.Empty;

        //        if (defaultScript == rule.InvariantScript)
        //        {
        //            continue;
        //        }                

        //        var script = rule.InvariantScript.Replace("\r\n", "\n").Replace("\r", "\n");

        //        var isMultiline = script.Contains("\n");
        //        if (isMultiline)
        //        {
        //            WriteMultilineRule(rule.Property, script);
        //        }
        //        else
        //        {
        //            WriteLine(rule.Property + " " + PAConstants.PropertyDelimiterToken + " " + script);
        //        }
        //    }

        //    _sb.AppendLine();

        //    foreach (var child in control.Children)
        //    {
        //        WriteControl(child);
        //    }

        //    _indentLevel--;
        //}

        //private void WriteTypedNameNode(TypedNameNode node)
        //{

        //}

        //private void WriteMultilineRule(string property, string script)
        //{
        //    WriteLine(property + " " + PAConstants.PropertyDelimiterToken);
        //    _indentLevel++;

        //    foreach (var line in script.TrimStart().Split('\n'))
        //    {
        //        WriteLine(line.Trim('\n'));
        //    }
        //    _indentLevel--;
        //}
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
                return string.Concat(Enumerable.Repeat("    ", indentation - 1));
            }

        }

    }
}
