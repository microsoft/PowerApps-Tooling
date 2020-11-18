// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser
{
    // $$$ remove this class?
    internal class TokenStream
    {
        private class PositionDetails
        {
            private int _currentLine;
            private int _currentChar;
            private int _tokenStartLine; // The start of the current token.
            private int _tokenStartChar; // The start of the current token.

            private readonly string _fileName;

            public int AbsolutePosition { get; private set; }

            public PositionDetails(string fileName)
            {
                AbsolutePosition = 0;
                _currentLine = 0;
                _currentChar = 0;
                _tokenStartLine = 0;
                _tokenStartChar = 0;
                _fileName = fileName;
            }

            public void StartToken()
            {
                _tokenStartLine = _currentLine;
                _tokenStartChar = _currentChar;
            }

            public SourceLocation GetSpan()
            {
                return new SourceLocation(_tokenStartLine, _tokenStartChar, _currentLine, _currentChar, _fileName);
            }

            public void Next()
            {
                AbsolutePosition++;
                _currentChar++;
            }

            public void AddIndent(int count)
            {
                AbsolutePosition += count;
                _currentChar += count;
            }

            public void  NewLine()
            {
                _currentChar = 0;
                _currentLine++;
            }
        }

        private readonly string _text;
        private readonly int _charCount;
        private PositionDetails _position;
        private readonly StringBuilder _sb;
        private Stack<int> _indentationLevel; // space count expected per block level

        private Stack<Token> _pendingTokens;

        public TokenStream(string text, string fileName)
        {
            _sb = new StringBuilder();
            _indentationLevel = new Stack<int>();
            _indentationLevel.Push(0);

            _pendingTokens = new Stack<Token>();

            // Normalize line endings
            _text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            _charCount = _text.Length;

            _position = new PositionDetails(fileName);
        }

        public void ValidateHeader()
        {
            var header = PAConstants.Header;
            if (!_text.StartsWith(header))
            {
                throw new InvalidOperationException($"Illegal pa source file. Missing header");
            }
            for (int i = 0; i <= header.Length; i++) { 
                NextChar();
            }
        }

        public bool Eof => _position.AbsolutePosition >= _charCount;
        private char CurrentChar => _position.AbsolutePosition < _charCount ? _text[_position.AbsolutePosition] : '\0';
        private char PreviousChar => _position.AbsolutePosition > 1 ? _text[_position.AbsolutePosition - 1] : '\0';

        private char NextChar()
        {
            if (_position.AbsolutePosition >= _charCount)
                throw new InvalidOperationException();

            _position.Next();
            if (_position.AbsolutePosition < _charCount)
                return _text[_position.AbsolutePosition];

            return '\0';
        }

        private char PeekChar(int i)
        {
            if (_position.AbsolutePosition + i >= _charCount)
                throw new InvalidOperationException();
            i += _position.AbsolutePosition;

            return (i < _charCount) ? _text[i] : '\0';
        }

        public void ReplaceToken(Token tok)
        {
            _pendingTokens.Push(tok);
        }


        public Token GetNextToken(bool expectedExpression = false)
        {
            for (; ; )
            {
                if (Eof)
                    return null;
                if (_pendingTokens.Any())
                    return _pendingTokens.Pop();

                var tok = Dispatch(expectedExpression);
                if (tok != null)
                    return tok;
            }
        }

        private Token Dispatch(bool expectedExpression)
        {
            _position.StartToken();
            if (expectedExpression)
            {
                return GetExpressionToken();
            }

            var ch = CurrentChar;
            if (IsNewLine(ch))
                return SkipNewLines();
            if (CharacterUtils.IsLineTerm(PreviousChar))
            {
                var tok = GetIndentationToken();
                if (tok != null)
                    return tok;
            }
            if (CharacterUtils.IsIdentStart(ch))
                return GetIdentOrKeyword();
            if (CharacterUtils.IsSpace(ch))
                return SkipSpaces();

            return GetPunctuator();

            // Add comment support
        }

        private bool IsNewLine(char ch)
        {
            return ch == '\n';
        }

        private Token GetExpressionToken()
        {
            if (CurrentChar == ' ')
                NextChar();
            var ch = CurrentChar;
            if (IsNewLine(ch))
            {
                _position.NewLine();
                return GetMultiLineExpressionToken();
            }
            else
                return GetSingleLineExpressionToken();
        }

        private Token GetMultiLineExpressionToken()
        {
            NextChar();
            var indentMin = PeekCurrentIndentationLevel();
            _sb.Length = 0;
            if (indentMin <= _indentationLevel.Peek())
                return new Token(TokenKind.PAExpression, _position.GetSpan(), _sb.ToString());
            StringBuilder lineBuilder = new StringBuilder();
            var lineIndent = PeekCurrentIndentationLevel();
            var newLine = false;

            while (indentMin <= lineIndent)
            {
                _position.AddIndent(indentMin - 1);
                if (newLine)
                    _sb.AppendLine();

                lineBuilder.Length = 0;
                while (!IsNewLine(NextChar()))
                {
                    lineBuilder.Append(CurrentChar);
                }
                _sb.Append(lineBuilder.ToString());
                if (IsNewLine(CurrentChar))
                {
                    NextChar();
                    _position.NewLine();
                }
                lineIndent = PeekCurrentIndentationLevel();
                newLine = true;
            }
            return new Token(TokenKind.PAExpression, _position.GetSpan(), _sb.ToString());
        }

        private Token GetSingleLineExpressionToken()
        {
            _sb.Length = 0;
            _sb.Append(CurrentChar);
            // Advance to end of line
            while (!IsNewLine(NextChar()))
            {
                _sb.Append(CurrentChar);
            }

            return new Token(TokenKind.PAExpression, _position.GetSpan(), _sb.ToString());
        }

        private Token SkipSpaces()
        {
            while (CharacterUtils.IsSpace(CurrentChar))
            {
                NextChar();
            }
            return null;
        }
        private Token SkipNewLines()
        {
            while (IsNewLine(CurrentChar))
            {
                _position.NewLine();
                NextChar();
            }
            return null;
        }

        private Token GetPunctuator()
        {
            int punctuatorLength = 0;
            _sb.Length = 0;
            _sb.Append(CurrentChar);
            TokenKind kind = TokenKind.None;
            string content = "";
            for (; ; )
            {
                content = _sb.ToString();
                if (!TryGetPunctuator(content, out TokenKind maybeKind))
                    break;

                kind = maybeKind;

                ++punctuatorLength;
                _sb.Append(PeekChar(_sb.Length));
            }

            while (punctuatorLength-- > 0)
                NextChar();
            return new Token(kind, _position.GetSpan(), content.Substring(0, _sb.Length-1));
        }

        private bool TryGetPunctuator(string maybe, out TokenKind kind)
        {
            switch (maybe)
            {
                case PAConstants.PropertyDelimiterToken:
                    kind = TokenKind.PropertyStart;
                    return true;
                case PAConstants.ControlTemplateSeparator:
                    kind = TokenKind.TemplateSeparator;
                    return true;
                case PAConstants.ControlVariantSeparator:
                    kind = TokenKind.VariantSeparator;
                    return true;
                default:
                    kind = TokenKind.None;
                    return false;
            }
        }

        private Token GetIdentOrKeyword()
        {
            var tokContents = GetIdentCore(out bool hasDelimiterStart, out bool hasDelimiterEnd);
            var span = _position.GetSpan();

            // Don't parse as keyword if there are delimiters
            if (IsKeyword(tokContents) && !hasDelimiterStart)
            {
                if (tokContents == PAConstants.ControlKeyword)
                    return new Token(TokenKind.Control, span, tokContents);
                else
                    throw new NotImplementedException("Keyword Token Error");
            }

            if (hasDelimiterStart && !hasDelimiterEnd)
                throw new NotImplementedException("Mismatched Ident Delimiter Error");

            return new Token(TokenKind.Identifier, span, tokContents);
        }

        private string GetIdentCore(out bool hasDelimiterStart, out bool hasDelimiterEnd)
        {
            _sb.Length = 0;
            hasDelimiterStart = CharacterUtils.IsIdentDelimiter(CurrentChar);
            hasDelimiterEnd = false;

            if (!hasDelimiterStart)
            {
                // Simple identifier.
                while (CharacterUtils.IsSimpleIdentCh(CurrentChar))
                {
                    _sb.Append(CurrentChar);
                    NextChar();
                }

                return _sb.ToString();
            }

            // Delimited identifier.
            NextChar();

            // Accept any characters up to the next unescaped identifier delimiter.
            // String will be corrected in the IdentToken if needed.
            for (; ; )
            {
                if (Eof)
                    break;
                if (CharacterUtils.IsIdentDelimiter(CurrentChar))
                {
                    if (CharacterUtils.IsIdentDelimiter(PeekChar(1)))
                    {
                        // Escaped delimiter.
                        _sb.Append(CurrentChar);
                        NextChar();
                        NextChar();
                    }
                    else
                    {
                        // End of the identifier.
                        NextChar();
                        hasDelimiterEnd = true;
                        break;
                    }
                }
                else if (IsNewLine(CurrentChar))
                {
                    // Terminate an identifier on a new line character
                    // Don't include the new line in the identifier
                    hasDelimiterEnd = false;
                    break;
                }
                else
                {
                    _sb.Append(CurrentChar);
                    NextChar();
                }
            }

            return _sb.ToString();
        }

        private int PeekCurrentIndentationLevel()
        {
            Contract.Assert(IsNewLine(PreviousChar));

            var indentation = 0;
            var ch = CurrentChar;
            while (ch == ' ')
            {
                ++indentation;
                ch = PeekChar(indentation);
            }

            return indentation;
        }

        // Returns null if no indentation change
        private Token GetIndentationToken()
        {
            var currentIndentation = _indentationLevel.Peek();
            var indentation = PeekCurrentIndentationLevel();
            _position.AddIndent(indentation);

            if (indentation == currentIndentation)
                return null;

            if (indentation > currentIndentation)
            {
                _indentationLevel.Push(indentation);
                return new Token(TokenKind.Indent, _position.GetSpan(), new string(' ', indentation));
            }

            // Dedent handling
            while (_indentationLevel.Peek() > indentation)
            {
                _indentationLevel.Pop();
                _pendingTokens.Push(new Token(TokenKind.Dedent, _position.GetSpan(), new string(' ', indentation)));
            }

            if (indentation == _indentationLevel.Peek())
            {
                return _pendingTokens.Pop();
            }

            throw new NotImplementedException( "Dedent mismatch error");
        }
        private static bool IsKeyword(string str)
        {
            return PAConstants.ControlKeyword == str;
        }

    }
}
