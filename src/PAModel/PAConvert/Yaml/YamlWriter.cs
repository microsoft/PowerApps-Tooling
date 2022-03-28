// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml
{
    /// <summary>
    /// Helper to write out a safe subset of Yaml.
    /// Notably, property values are always either multi-line escaped or
    /// prefixed with '=' to block yaml from treating it as a yaml expression.
    /// </summary>
    public class YamlWriter
    {
        private readonly TextWriter _text;
        private int _currentIndent;

        private const string Indent = "    ";

        public YamlWriter(TextWriter text)
        {
            _text = text;
        }

        public void WriteStartObject(string propertyName)
        {
            WriteIndent();

            bool needsEscape = propertyName.IndexOfAny(new char[] { '\"', '\'' }) != -1;
            if (needsEscape)
                propertyName = $"\"{propertyName.Replace("\"", "\\\"")}\"";

            _text.Write(propertyName);
            _text.WriteLine(":");

            _currentIndent++;
        }
        public void WriteEndObject()
        {
            _currentIndent--;
            if (_currentIndent < 0)
            {
                throw new InvalidOperationException("No matching start object");
            }
        }

        public void WriteProperty(string propertyName, bool value)
        {
            WriteIndent();
            _text.Write(propertyName);
            _text.Write(": ");
            _text.WriteLine(value ? "true" : "false");
        }

        public void WriteProperty(string propertyName, int value)
        {
            WriteIndent();
            _text.Write(propertyName);
            _text.Write(": ");
            _text.WriteLine(value);
        }

        public void WriteProperty(string propertyName, double value)
        {
            WriteIndent();
            _text.Write(propertyName);
            _text.Write(": ");
            _text.WriteLine(value);
        }

        /// <summary>
        /// Safely write a property. Based on the value, will chose whether single-line (and prefix with an '=')
        /// or multi-line and pick the right the escape. 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void WriteProperty(string propertyName, string value, bool includeEquals = true)
        {
            if (value == null)
            {
                WriteIndent();
                _text.Write(propertyName);
                _text.WriteLine(":");
                return;
            }

            value = NormalizeNewlines(value);

            bool isSingleLine = value.IndexOfAny(new char[] { '#', '\n', ':' }) == -1;

            // For consistency, both single and multiline PA properties prefix with '='.
            // Only single-line actually needs this - to avoid yaml's regular expression escaping.
            if (includeEquals)
            {
                value = '=' + value;
            }

            if (isSingleLine)
            {
                WriteIndent();
                _text.Write(propertyName);
                _text.Write(": ");
                _text.WriteLine(value);
            }
            else
            {
                WriteIndent();
                _text.Write(propertyName);
                _text.Write(": ");

                int numNewlines = 0;
                for (int i = value.Length - 1; i > 0; i--)
                {
                    if (value[i] == '\n')
                    {
                        numNewlines++;
                    }
                    else
                    {
                        break;
                    }
                }
                switch (numNewlines)
                {
                    case 0: _text.WriteLine("|-"); break;
                    case 1: _text.WriteLine("|"); break;
                    default: _text.WriteLine("|+"); break;
                }

                _currentIndent++;

                bool needIndent = true;
                foreach (var ch in value)
                {
                    if (needIndent)
                    {
                        WriteIndent();
                        needIndent = false;
                    }

                    if (ch == '\n')
                    {
                        _text.WriteLine();
                        needIndent = true;
                        continue;
                    }
                    _text.Write(ch);
                }

                if (numNewlines == 0)
                {
                    _text.WriteLine();
                }

                _currentIndent--;
            }
        }

        // Write a newline, for aesthics. since this is inbetween properties,
        // it should get ignored on parse. 
        public void WriteNewline()
        {
            _text.WriteLine();
        }

        private void WriteIndent()
        {
            for (int i = 0; i < _currentIndent; i++)
            {
                _text.Write(Indent);
            }
        }

        private string NormalizeNewlines(string x)
        {
            return x.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
