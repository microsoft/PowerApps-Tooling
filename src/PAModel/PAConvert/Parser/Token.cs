// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser
{
    internal class Token
    {
        public Token(TokenKind kind, SourceLocation span, string content)
        {
            Kind = kind;
            Span = span;
            Content = content;
        }

        public TokenKind Kind { get; }
        public TokenSpan Span { get; }
        public string Content { get; }

        public override bool Equals(object obj)
        {
            return obj is Token other &&
                other.Kind == Kind &&
                other.Span.Min == Span.Min &&
                other.Span.Lim == Span.Lim &&
                other.Content == Content;
        }

        public override int GetHashCode()
        {
            return (Kind, Span, Content).GetHashCode();
        }
    }
}
