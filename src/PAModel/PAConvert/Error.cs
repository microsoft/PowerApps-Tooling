// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools;

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

    public bool IsError => Code.IsError();
    public bool IsWarning => !IsError;

    public override string ToString()
    {
        var sb = new StringBuilder();
        WriteTo(sb);
        return sb.ToString();
    }

    internal void WriteTo(StringBuilder sb)
    {
        // Format using VS error format
        // 1>E:\repos\github\microsoft\PA-Tooling\src\PAModel\PAConvert\Error.cs(42,11,42,11): error CS1002: ; expected
        var origLen = sb.Length;
        Span.WriteTo(sb);
        if (sb.Length != origLen)
        {
            sb.Append(": ");
        }

        if (IsError)
        {
            sb.Append("Error   ");
        }
        else
        {
            sb.Append("Warning ");
        }

        sb.Append($"PA{(int)Code}: ");
        sb.Append(Message);
    }
}
