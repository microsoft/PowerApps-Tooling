// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// Helper to read our strict subset of Yaml.
/// Give useful errors on things outside of the subset.
/// See https://yaml.org/
/// This will strip '=' signs at the start of single property values. 
/// </summary>
internal class YamlLexer : IDisposable
{
    private const string NewLine = "\n";

    // The actual contents to read. 
    private readonly TextReader _reader;

    // Stack of indentations. 
    private readonly Stack<Indent> _currentIndent;

    private YamlToken _lastPair;

    public bool IsComponent { get; set; }

    // for error handling
    private readonly string _currentFileName;

    private string _currentLineContents;

    // Per https://github.com/microsoft/PowerApps-Language-Tooling/issues/115,
    // We allow comments, but don't round-trip them. Issue a warning. 
    public SourceLocation? _commentStrippedWarning;
    private bool _isDisposed;
    public const string MissingSingleQuoteFunctionNode = "Missing closing \' in Function Node";
    public const string MissingSingleQuoteComponent = "Missing closing \' in Component";
    public const string MissingSingleQuoteTypeNode = "Missing closing \' in TypeNode";
    public const string MissingSingleQuoteProperty = "Missing closing \' in Property";

    public YamlLexer(TextReader source, string filenameHint = null)
    {
        _reader = source;
        _currentFileName = filenameHint; // For error reporting

        // We pretend the file is wrapped in a "object:" tag.
        _lastPair = YamlToken.NewStartObj(default, null);
        _currentIndent = new Stack<Indent>()
        {
            new() {
                _oldIndentLevel = -1,
                _lineStart = 0
            }
        };
    }

    /// <summary>
    /// Current line number. 1-based.
    /// </summary>
    public int CurrentLine { get; private set; }

    public YamlLexerOptions Options { get; set; } = YamlLexerOptions.EnforceLeadingEquals;

    private LineParser PeekLine()
    {
        if (_currentLineContents == null)
        {
            _currentLineContents = _reader.ReadLine();
            if (_currentLineContents == null)
            {
                return null;
            }
            CurrentLine++;
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

        LineParser line;
        int indentLen;
        while (true)
        {
            line = PeekLine();

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
            indentLen = line.EatIndent();

            // Comment indent level doesn't matter - may not match object indent. 
            if (line.Current == '#')
            {
                // https://github.com/microsoft/PowerApps-Language-Tooling/issues/115
                // Allow comment lines. These will get stripped (won't roundtrip).
                if (!_commentStrippedWarning.HasValue)
                {
                    _commentStrippedWarning = Loc(line);
                }

                MoveNextLine();
                continue;
            }

            if (line.Current != 0)
            {
                break;
            }

            // Eat the newline and go to the next line. 
            MoveNextLine();
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

        var startColumn = line._idx + 1; // 1-based start column number.


        // If last was 'start object', then this indent sets the new level,
        if (_lastPair.Kind == YamlTokenKind.StartObj)
        {
            var lastIndent = _currentIndent.Peek()._oldIndentLevel;

            if (indentLen == lastIndent) // Close immediate parent
            {
                _currentIndent.Pop();
                return YamlToken.EndObj;
            }
            else if (indentLen < lastIndent)
            {
                // Close current objects one at a time.                     
                _currentIndent.Pop();

                var prevIndent = _currentIndent.Peek()._oldIndentLevel;
                // Indent must exactly match a previous one up the stack. 
                if (indentLen > prevIndent)
                {
                    return Error(line, "Property indent must align exactly with a previous indent level.");
                }

                return YamlToken.EndObj;
            }

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
        // toggle isEscaped on every ' appearance
        var isEscaped = false;
        var requiresClosingDoubleQuote = line.MaybeEat('"');

        while (line.Current != ':' || isEscaped || requiresClosingDoubleQuote)
        {
            if (line.Current == '\'')
            {
                isEscaped = !isEscaped;
            }

            if (requiresClosingDoubleQuote && line.Current == '"' && line.Previous != '\\')
            {
                line._idx++;
                break;
            }

            if (line.Current == 0) // EOL
            {
                if (isEscaped)
                    return UnsupportedSingleQuote(line, IsComponent);

                if (requiresClosingDoubleQuote)
                    return Unsupported(line, "Missing closing \".");

                return Unsupported(line, "Missing ':'. If this is a multiline property, use |");
            }
            line._idx++;
        }
        line.MaybeEat(':'); // skip colon. 

        // Prop name could have spaces, but no colons. 
        var propName = line._line.Substring(indentLen, line._idx - indentLen - 1).Trim();

        if (requiresClosingDoubleQuote)
        {
            propName = propName.Replace("\\\"", "\"").Trim('"');
        }

        // If it's a property, must have at least 1 space.
        // If it's start object, then ignore all spaces. 

        var iSpaces = 0;
        while (line.MaybeEat(' '))
        {
            iSpaces++; // skip optional spaces
        }

        YamlToken error;
        if (line.Current == 0) // EOL
        {
            error = _currentIndent.Peek().CheckDuplicate(propName, CurrentLine);
            if (error != null)
            {
                error.Span = Loc(line);
                return error;
            }

            // New Object.
            // Next line must begin an indent.
            _currentIndent.Push(new Indent
            {
                _lineStart = CurrentLine,
                _oldIndentLevel = indentLen
                // newIndentLevel will be set at next line
            });

            MoveNextLine();
            return YamlToken.NewStartObj(Loc(startColumn, line), propName);
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

        string value;
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
            // Is "hello" a string or identifier? 
            //    Foo: "Hello"
            //
            // Instead, have the user write:
            //    Foo: ="Hello"  // String
            //    Foo: =Hello    // identifier
            //    Foo: |
            //      ="Hello"     // string
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

            if (value[0] != '=')
            {
                return Unsupported(line, "Property value must start with an '='");
            }

            value = value.Substring(1); // move past '='
        }
        else if (Options.HasFlag(YamlLexerOptions.EnforceLeadingEquals))
        {
            // Warn on legal yaml escapes (>) that we don't support in our subset here. 
            return Error(line, "Expected either '=' for a single line expression or '|' to begin a multiline expression");
        }
        else
        {
            // Single line. Property doesn't include \n at end.
            value = line.RestOfLine;
            MoveNextLine();
        }

        error = _currentIndent.Peek().CheckDuplicate(propName, CurrentLine);
        if (error != null)
        {
            error.Span = Loc(line);
            return error;
        }

        var endIndex = line._line.Length + 1;
        return YamlToken.NewProperty(LocWorker(startColumn, endIndex), propName, value);
    }


    // Errors that are valid yaml, but not in our supported subset. 
    private YamlToken Unsupported(LineParser line, string message)
    {
        return Error(line, message);
    }

    private YamlToken UnsupportedSingleQuote(LineParser line, bool isComponent)
    {
        var lineSplit = line._line.ToString().ToLower().Split(new string[] { " as " }, StringSplitOptions.None);

        if (lineSplit.Length > 2)
        {
            return Error(line, MissingSingleQuoteFunctionNode);
        }
        if (lineSplit.Length > 1)
        {
            return isComponent ? Error(line, MissingSingleQuoteComponent) : Error(line, MissingSingleQuoteTypeNode);
        }
        return Error(line, MissingSingleQuoteProperty);
    }

    private YamlToken UnsupportedComment(LineParser line)
    {
        return Unsupported(line, "# comments are not supported. Use // within a property to add comments.");
    }

    private YamlToken Error(LineParser line, string message)
    {
        return YamlToken.NewError(Loc(line), message);
    }


    // For an error at a specific character. 
    private SourceLocation Loc(LineParser line)
    {
        var columnIdx1 = line._idx + 1;
        return LocWorker(columnIdx1, columnIdx1);
    }

    // For a success case referring to a range.
    private SourceLocation Loc(int startIndex1, LineParser endChar)
    {
        var endIndex1 = endChar._idx + 1; // convert 0-based to 1-base 
        return LocWorker(startIndex1, endIndex1);
    }

    // 1-based indexes. 
    private SourceLocation LocWorker(int startIndex1, int endIndex1)
    {
        return new SourceLocation(CurrentLine, startIndex1, CurrentLine, endIndex1, _currentFileName);
    }

    // https://yaml-multiline.info/

    private string ReadMultiline(int multilineMode)
    {
        var sb = new StringBuilder();

        // First line establishes indent level.
        // Yaml allows empty Multilines, we require it must have at least 1 line in it. (no empty values) 

        var parentIndent = _currentIndent.Peek()._newIndentLevel;
        var thisIndent = -1;

        while (true)
        {
            var line = PeekLine();
            if (line == null)
            {
                break; // end of file. 
            }
            var indentLen = line.EatIndent();
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
            MoveNextLine();
        }

        var val = sb.ToString();
        if (multilineMode == 0)
        {
            while (true)
            {
                if (sb.Length > 0 && sb[^1] == '\n')
                {
                    sb.Length--;
                }
                else
                {
                    break;
                }
                if (sb.Length > 0 && sb[^1] == '\r')
                {
                    sb.Length--;
                }
                else
                {
                    break;
                }
            }
            // Trim any newlines.
            val = sb.ToString();

        }
        else if (multilineMode == 1)
        {
            // Just one. This is already the case. 
        }
        else if (multilineMode > 1)
        {
            // Allows multiple. 
        }

        // End of multiline escape.
        return val;
    }

    // Helper for reading through a single line.
    // This treats EOL as (char) 0.
    [DebuggerDisplay("{DebuggerToString()}")]
    private class LineParser
    {
        // 0-based character index into line 
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
        public char Previous
        {
            get
            {
                if (_idx - 1 >= _line.Length || _idx - 1 < 0) { return (char)0; }
                return _line[_idx - 1];
            }
        }


        public string RestOfLine
        {
            get
            {
                if (_idx >= _line.Length) return string.Empty;
                return _line.Substring(_idx);
            }
        }


        public bool MaybeEat(char ch)
        {
            if (Current == ch)
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
            while (MaybeEat(' ')) ;
            return _idx;
        }

        // Show '|' at _idx position
        private string DebuggerToString()
        {
            var idx = Math.Min(_line.Length, _idx);
            return _line.Substring(0, idx) + "¤" + _line.Substring(idx);
        }
    }

    // Indentation levels within the file.
    // These are maintained in a stack.
    [DebuggerDisplay("Start: {_lineStart}, {_oldIndentLevel}-->{_newIndentLevel}")]
    private class Indent
    {
        public int _oldIndentLevel;
        public int _newIndentLevel; // for children of this object. 
        public int _lineStart; // what line did this indenting start at?

        // For detecting collisions in previous properties.
        // Collisions must be *case-sensitive*
        // Map property name to line that it was declared on. 
        public Dictionary<string, int> _previousProperties = new(StringComparer.Ordinal);

        internal YamlToken CheckDuplicate(string propName, int currentLine)
        {
            if (_previousProperties.TryGetValue(propName, out var oldLine))
            {
                // Key is already present.
                return YamlToken.NewError(default, $"Property was already defined on line {oldLine} and now on line {currentLine}.");
            }
            _previousProperties.Add(propName, currentLine);
            return null;
        }
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _reader?.Dispose();
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}



