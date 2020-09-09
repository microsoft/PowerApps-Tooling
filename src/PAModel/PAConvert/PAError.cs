// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PAModel.PAConvert.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace PAModel.PAConvert
{
    public class PAError
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
