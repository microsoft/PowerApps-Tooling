// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml
{
    /// <summary>
    /// Helper to write out a safe subset of Yaml.
    /// Notably, property values are always either multi-line escaped or
    /// prefixed with '=' to block yaml from treating it as a yaml expression.
    /// </summary>
    internal class YamlWriter
    {
        private readonly TextWriter _text;
        private int _currentIndent;

        private const string Indent = "  ";

        public YamlWriter(TextWriter text)
        {
            _text = text;
        }

        public void WriteStartObject(string propertyName)
        {
            WriteIndent();
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

        public void WriteQuotedSingleLinePair(string propertyName, string value)
        {
            if (value == null)
            {
                WriteIndent();
                _text.Write(propertyName);
                _text.WriteLine(":");
                return;
            }

            WriteIndent();
            _text.Write(propertyName);
            _text.Write(": \"");
            _text.Write(EscapeString(value));
            _text.WriteLine("\"");
        }

        private string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Safely write a property. Based on the value, will chose whether single-line (and prefix with an '=')
        /// or multi-line and pick the right the escape. 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void WriteProperty(string propertyName, string value)
        {
            if (value == null)
            {
                WriteIndent();
                _text.Write(propertyName);
                _text.WriteLine(":");
                return;
            }

            bool isSingleLine = value.IndexOfAny(new char[] { '#', '\n', ':' }) == -1;

            // For consistency, both single and multiline PA properties prefix with '='.
            // Only single-line actually needs this - to avoid yaml's regular expression escaping.
            value = '=' + value;

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
                    // Let \r pass through and write normally. 
                    if (ch == '\n')
                    {
                        _text.Write(ch); // writes same type of newlinw
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
    }
}
