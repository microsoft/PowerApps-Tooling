// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class PAWriter
    {
        private int _indentLevel;
        private string IndentString { get { return new string(' ', _indentLevel * 4); } }

        private StringBuilder _sb;
        private Dictionary<string, ControlTemplate> _templates;
        private readonly Theme _theme;

        public PAWriter(StringBuilder sb, Dictionary<string, ControlTemplate> templates, Theme theme)
        {
            _indentLevel = 0;
            _sb = sb;
            _templates = templates;
            _theme = theme;
        }
        private void WriteLine(string line)
        {
            _sb.AppendLine(IndentString + line);
        }

        public void WriteControl(ControlInfoJson.Item control, bool isComponent = false)
        {
            var controlTemplate = CharacterUtils.EscapeName(control.Template.Name);
            if (control.VariantName.Length > 0)
                controlTemplate += $"{PAConstants.ControlVariantSeparator} {CharacterUtils.EscapeName(control.VariantName)}";

            WriteLine($"{(isComponent ? PAConstants.ComponentKeyword : PAConstants.ControlKeyword)} {CharacterUtils.EscapeName(control.Name)} {PAConstants.ControlTemplateSeparator} {controlTemplate}");
            _indentLevel++;

            _templates.TryGetValue(control.Template.Name, out var template);

            var defaulter = new DefaultRuleHelper(control, template, _theme);
                
            foreach (var rule in control.Rules)
            {
                if (!defaulter.TryGetDefaultRule(rule.Property, out var defaultScript))
                    defaultScript = string.Empty;
                    
                if (defaultScript == rule.InvariantScript)
                {
                    continue;
                }                

                var script = rule.InvariantScript.Replace("\r\n", "\n").Replace("\r", "\n");

                var isMultiline = script.Contains("\n");
                if (isMultiline)
                {
                    WriteMultilineRule(rule.Property, script);
                }
                else
                {
                    WriteLine(rule.Property + " " + PAConstants.PropertyDelimiterToken + " " + script);
                }
            }

            _sb.AppendLine();

            foreach (var child in control.Children)
            {
                WriteControl(child);
            }

            _indentLevel--;
        }

        private void WriteMultilineRule(string property, string script)
        {
            WriteLine(property + " " + PAConstants.PropertyDelimiterToken);
            _indentLevel++;

            foreach (var line in script.TrimStart().Split('\n'))
            {
                WriteLine(line.Trim('\n'));
            }
            _indentLevel--;
        }
    }
}
