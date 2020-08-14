using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace PAModel.PAConvert
{
    internal class PAWriter
    {
        private int _indentLevel;
        private string IndentString { get { return new string(' ', _indentLevel * 4); } }

        private const string PropertyDelimiterToken = " :=";

        private StringBuilder _sb;


        public PAWriter(StringBuilder sb)
        {
            _indentLevel = 0;
            _sb = sb;
        }
        private void WriteLine(string line)
        {
            _sb.AppendLine(IndentString + line);
        }

        public void WriteControl(ControlInfoJson.Item control)
        {
            WriteLine($"control {control.Name} : {control.Template.Name}");
            _indentLevel++;

            foreach (var rule in control.Rules)
            {
                var isMultiline = rule.InvariantScript.Contains("\n");
                if (isMultiline)
                {
                    WriteMultilineRule(rule);
                }
                else
                {
                    WriteLine(rule.Property + PropertyDelimiterToken + " " + rule.InvariantScript);
                }
            }

            _sb.AppendLine();

            foreach (var child in control.Children)
            {
                WriteControl(child);
            }

            _indentLevel--;
        }

        private void WriteMultilineRule(ControlInfoJson.RuleEntry rule)
        {
            WriteLine(rule.Property + PropertyDelimiterToken);
            _indentLevel++;

            foreach (var line in rule.InvariantScript.Split('\n'))
            {
                WriteLine(line.Trim('\r','\n'));
            }
            _indentLevel--;
        }
    }
}
