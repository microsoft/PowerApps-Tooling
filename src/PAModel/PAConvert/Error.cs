// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// An Error or warning encountered while doing a source operation. 
    /// </summary>
    public class Error
    {
        internal ErrorCode Code;
        internal SourceLocation Span;

        public string Message;

        internal Error(ErrorCode code, SourceLocation span, string message)
        {
            Span = span;
            Message = message;
            Code = code;
        }

        public bool IsError => this.Code.IsError();
        public bool IsWarning => !IsError;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.IsError) {
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
