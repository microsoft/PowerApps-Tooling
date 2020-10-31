// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class PAError
    {
        public SourceLocation Span;
        public string Message;

        public PAError(SourceLocation span, string message)
        {
            Span = span;
            Message = message;
        }
    }
}
