// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    public class PAError
    {
        public ErrorCode Code;
        internal SourceLocation Span;
        public string Message;

        internal PAError(ErrorCode code, SourceLocation span, string message)
        {
            Span = span;
            Message = message;
            Code = code;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.Code.IsError()) {
                sb.Append("Error   ");
            } else {
                sb.Append("Warning ");
            }
            sb.Append($"PA{(int)Code}: ");
            sb.Append(this.Message);

            if (this.Span.FileName != null)
            {
                sb.Append(" at ");
                sb.Append(this.Span);
            }
            return sb.ToString();
        }
    }
}
