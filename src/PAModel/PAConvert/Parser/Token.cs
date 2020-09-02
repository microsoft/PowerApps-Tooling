using System;
using System.Collections.Generic;
using System.Text;

namespace PAModel.PAConvert.Parser
{
    internal class Token
    {
        public Token(TokenKind kind, TokenSpan span, string content)
        {
            Kind = kind;
            Span = span;
            Content = content;
        }

        public TokenKind Kind { get; }
        public TokenSpan Span { get; }
        public string Content { get; } 
    }
}
