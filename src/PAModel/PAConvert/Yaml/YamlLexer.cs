// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml
{
    internal enum YamlTokenKind
    {
        /// <summary>
        /// A property. Could be 
        /// </summary>
        Property,

        /// <summary>
        /// The start of an object. 
        /// </summary>
        StartObj,

        /// <summary>
        /// End of an object. Matches with StartObj. 
        /// </summary>
        EndObj,

        /// <summary>
        /// End of the file
        /// </summary>
        EndOfFile,

        /// <summary>
        /// Represents a lex error. 
        /// </summary>
        Error
    }

    /// <summary>
    /// A yaml token in the file. 
    /// </summary>
    internal class YamlToken
    {
        internal static YamlToken EndObj = new YamlToken { Kind = YamlTokenKind.EndObj };       
        internal static YamlToken EndOfFile = new YamlToken { Kind = YamlTokenKind.EndOfFile };

        public YamlTokenKind Kind { get; set; }

        /// <summary>
        /// The name of a property. Valid for Property and StartObj kinds. 
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The contents of a Property. Valid for Property kinds. 
        /// </summary>
        public string Value { get; set; }

        public static YamlToken NewError(SourceLocation span, string message)
        {
            return new YamlToken
            {
                Kind = YamlTokenKind.Error,
                Span = span,
                Value = message
            };
        }

        // Used for error reporting. 
        public SourceLocation Span { get; set; }

        public override string ToString()
        {
            switch (Kind)
            {
                case YamlTokenKind.Property: return $"{Property}={Value}";
                case YamlTokenKind.StartObj: return $"{Property}:";
                case YamlTokenKind.Error: return $"Error: {Value}";
                default:
                    return $"<{Kind}>";
            }
        }
    }

    /// <summary>
    /// Helper to read our strict subset of Yaml.
    /// Give useful errors on things outside of the subset.
    /// See https://yaml.org/
    /// This will strip '=' signs at the start of single property values. 
    /// </summary>
    internal class YamlLexer
    {
        private static string NewLine = "\r\n";

        // The actual contents to read. 
        private readonly TextReader _reader;
        private readonly string _currentFileName;

        // Stack of indentations. 
        private readonly Stack<Indent> _currentIndent;

        private YamlToken _lastPair;

        private int _currentLine;
        private string _currentLineContents = null;

        public YamlLexer(TextReader source, string filenameHint = null)
        {
            _reader = source;
            _currentFileName = filenameHint; // For errort reporting

            // We pretend the file is wrapped in a "object:" tag.
            _lastPair = new YamlToken { Kind = YamlTokenKind.StartObj };
            _currentIndent = new Stack<Indent>()
            {
                new Indent
                {
                    _oldIndentLevel = -1,
                    _lineStart = 0
                }
            };
        }

        private SourceLocation Loc(LineParser line)
        {
            return Loc(line._idx, line._idx);
        }

        private SourceLocation Loc(int startChar, int endChar)
        {
            return new SourceLocation(_currentLine, startChar, _currentLine, endChar, _currentFileName);
        }
               
        private LineParser PeekLine()
        {
            if (_currentLineContents == null)
            {
                _currentLineContents = _reader.ReadLine();
                if (_currentLineContents == null)
                {
                    return null;
                }
                _currentLine++;
            }
            return new LineParser(_currentLineContents);
        }

        private void MoveNextLine()
        {
            _currentLineContents = null;
        }

        /// <summary>
        /// Get the next token in the stream. Returns an EOF at the end of document. 
        /// </summary>
        /// <returns></returns>
        public YamlToken ReadNext()
        {
            var pair = ReadNextWorker();
            _lastPair = pair;
            return pair;
        }

        private YamlToken ReadNextWorker()
        {
            // Warn on:
            //   # comment
            //   no escape (= or |)
            //   unallowed multiline escape
            //   blank lines
            //   different indent size
            //   duplicate keys
            //   empty properties (should have an indent)
            //   no tabs. 
            //  don't allow --- documents 

            // Start of line.
            // [Indent] [PropertyName] [Colon]
            // [Indent] [PropertyName] [Colon] [space] [equals] [VALUE] 
            // [Indent] [PropertyName] [Colon] [space] [MultilineEscape]

Retry:
            LineParser line = PeekLine();

            if (line == null)
            {
                // End of File.
                // Close any  outstanding objects.
                if (_currentIndent.Count > 1)
                {
                    _currentIndent.Pop();
                    return YamlToken.EndObj;
                }
                return YamlToken.EndOfFile;
            }
            
            // get starting indent.
            int indentLen = line.EatIndent();

            if (line.Current == 0)
            {
                // return Unsupported(line, "Blank lines aren't supported");
                MoveNextLine();
                goto Retry;
            }

            if (line.Current == '-')
            {
                return Unsupported(line, "--- markers are not supported. Only a single document per file.");
            }

            if (line.Current == '\t')
            {
                // Helpful warning.
                return Error(line, "Use spaces, not tabs.");
            }


            // If last was 'start object', then this indent sets the new level,
            if (_lastPair.Kind == YamlTokenKind.StartObj)
            {
                var lastIndent = _currentIndent.Peek()._oldIndentLevel;

                if (indentLen <= lastIndent)
                {
                    // Error. new object should be indented.
                    return Unsupported(line, "Can't have null properties. Line should be indented to start a new property.");
                }
                _currentIndent.Peek()._newIndentLevel = indentLen;
            }
            else
            {
                // Subsequent properties should be at same indentation level.
                var expectedIndent = _currentIndent.Peek()._newIndentLevel;
                if (indentLen == expectedIndent)
                {
                    // Good. Common case.
                    // Continue processing below to actually parse this property. 
                }
                else if (indentLen > expectedIndent)
                {
                    return Error(line, "Property should be at same indent level");
                }
                else
                {
                    // Closing an object.
                    // Indent must exactly match a previous one up the stack. 
                    if (indentLen > _currentIndent.Peek()._oldIndentLevel)
                    {
                        return Error(line, "Property indent must align exactly with a previous indent level.");
                    }
                    
                    // Close current objects one at a time.                     
                    _currentIndent.Pop();
                    return YamlToken.EndObj;                    
                }
            }

            // Get identifier
            while(line.Current != ':')
            {
                if (line.Current == 0) // EOL
                {
                    return Unsupported(line, "Missing ':'. If this is a multiline property, use |");
                }
                line._idx++;
            }
            line.MaybeEat(':'); // skip colon. 

            // Prop name could have spaces, but no colons. 
            var propName = line._line.Substring(indentLen, line._idx - indentLen-1).Trim();

            // If it's a property, must have at least 1 space.
            // If it's start object, then ignore all spaces. 

            int iSpaces = 0;
            while (line.MaybeEat(' '))
            {
                iSpaces++; // skip optional spaces
            }

            if (line.Current == 0) // EOL
            {
                // New Object.
                // Next line must begin an indent.
                _currentIndent.Push(new Indent {
                        _lineStart = _currentLine,
                         _oldIndentLevel = indentLen
                         // newIndentLevel will be set at next line
                });

                MoveNextLine();
                return new YamlToken { Kind = YamlTokenKind.StartObj, Property = propName };
            }

            if (line.Current == '#')
            {
                return UnsupportedComment(line);
            }

            if (iSpaces == 0)
            {
                return Error(line, "Must have at least 1 space after colon.");
            }
                        

            // Escape must be

            string value = null;
            if (line.MaybeEat('='))
            {
                // Single line. Property doesn't include \n at end.
                value = line.RestOfLine;

                if (value.IndexOf('#') >= 0)
                {
                    return UnsupportedComment(line);
                }

                MoveNextLine();
            }
            else if ((line.Current == '\"') || (line.Current == '\''))
            {
                // These are common YAml sequences, but extremely problematic and could be user error.
                // Disallow them and force the user to explicit.
                // Is "hello" a string or identifer? 
                //    Foo: "Hello"
                //
                // Instead, have the user write:
                //    Foo: ="Hello"  // String
                //    Foo: Hello     // identifier
                //    Foo: |
                //         "Hello"   // string
                return Unsupported(line, "Quote is not a supported escape sequence. Use = or |");
            }
            else if (line.MaybeEat('>'))
            {
                // Unsupported Multiline escape.
                return Unsupported(line, "> is not a supported multiline escape. Use |");
            }
            else if (line.MaybeEat('|'))
            {
                // Multiline
                int multilineMode;
                if (line.MaybeEat('-'))
                {
                    multilineMode = 0; // 0 newlines at end
                }
                else if (line.MaybeEat('+'))
                {
                    multilineMode = 2; // 1+ newlines. 
                }
                else
                {
                    multilineMode = 1; // exactly 1 newline at end. 
                }

                iSpaces = 0;
                while (line.MaybeEat(' '))
                {
                    iSpaces++; // skip optional spaces
                }

                if (line.Current == '#')
                {
                    return UnsupportedComment(line);
                }
                else if (line.Current != 0) // EOL, catch all error. 
                {
                    return Error(line, "Content for | escape must start on next line.");
                }

                MoveNextLine();
                value = ReadMultiline(multilineMode);

                if (value.Length == 0)
                {
                    return Unsupported(line, "Can't have empty multiline expressions.");
                }
            }
            else
            {
                // Warn on legal yaml escapes (>) that we don't support in our subset here. 
                return Error(line, "Expected either '=' for a single line expression or '|' to begin a multiline expression");
            }

            return new YamlToken { Kind = YamlTokenKind.Property, Property = propName, Value = value };
        }


        // Errors that are valid yaml, but not in our supported subset. 
        private YamlToken Unsupported(LineParser line, string message)
        {
            return Error(line, message);
        }

        private YamlToken UnsupportedComment(LineParser line)
        {
            return Unsupported(line, "# comments are not supported. Use // within a property to add comments.");
        }

        private YamlToken Error(LineParser line, string message)
        {
            return YamlToken.NewError(this.Loc(line), message);
        }

        // https://yaml-multiline.info/

        private string ReadMultiline(int multilineMode)
        {
            StringBuilder sb = new StringBuilder();

            // First line establishes indent level.
            // Yaml allows empty Multilines, we require it must have at least 1 line in it. (no empty values) 

            int parentIndent = _currentIndent.Peek()._newIndentLevel;
            int thisIndent = -1;

            while (true)
            {
                LineParser line = PeekLine();
                if (line == null)
                {
                    break; // end of file. 
                }
                int indentLen = line.EatIndent();
                if (thisIndent == -1)
                {
                    // First line, sets the indent 
                    thisIndent = indentLen;
                }

                if (indentLen <= parentIndent)
                {
                    break;
                }

                line._idx = thisIndent;
                sb.Append(line.RestOfLine);
                sb.Append(NewLine);
                this.MoveNextLine();
            }

            var val = sb.ToString(); 
            if (multilineMode == 0)
            {
                while(true)
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                    {
                        sb.Length--;
                    } else
                    {
                        break;
                    }
                    if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
                    {
                        sb.Length--;
                    } else
                    {
                        break;
                    }
                }
                // Trim any newlines.
                val = sb.ToString();
                
            } else if (multilineMode == 1)
            {
                // Just one. This is already the case. 
            } else if (multilineMode > 1)
            {
                // Allows multiple. 
            }

            // End of multiline escape.
            return val;
        }

        // Helper for reading through a single line.
        // This treats EOL as (char) 0. 
        class LineParser
        {
            public int _idx;
            public readonly string _line;

            public LineParser(string line)
            {
                _line = line;
            }

            // Helper to handle eol. 
            public char Current
            {
                get
                {
                    if (_idx >= _line.Length) { return (char)0; }
                    return _line[_idx];
                }
            }

            public string RestOfLine
            {
                get
                {
                    if (_idx >= _line.Length) return string.Empty;
                    return this._line.Substring(_idx);
                }
            }


            public bool MaybeEat(char ch)
            {
                if (this.Current == ch)
                {
                    _idx++;
                    return true;
                }
                return false;
            }

            // Eat the left indent and return # of spaces in it.
            // We require spaces and don't allow tabs. 
            public int EatIndent()
            {
                while (this.MaybeEat(' ')) ;
                return this._idx;
            }
        }

        // Identation levels within the file.
        // These are maintained in a stack.
        [DebuggerDisplay("Start: {_lineStart}, {_oldIndentLevel}-->{_newIndentLevel}")]
        private class Indent
        {
            public int _oldIndentLevel;
            public int _newIndentLevel; // for children of this object. 
            public int _lineStart; // what line did this indenting start at?
        }
    }
}



