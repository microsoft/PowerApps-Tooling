// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Parser;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class PAError
    {
        public TokenSpan Span;
        public string Message;

        public PAError(TokenSpan span, string message)
        {
            Span = span;
            Message = message;
        }
    }
}
