// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Parser;
using System;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// Error codes from reading, writing (compiling) a document.
    /// These numbers must stay stable. 
    /// </summary>
    internal enum ErrorCode
    {
        // Warnings start at 2000
        ChecksumMismatch = 2001,

        // Catch-all - review and remove. 
        GenericWarning = 2999,

        // Errors start at 3000
        InternalError = 3001,

        FormatNotSupported = 3002,

        // Parse error 
        ParseError = 3003,

        // Catch-all.  Should review and make these more specific. 
        Generic = 3999,

    }

    internal static class ErrorCodeExtensions
    {
        public static bool IsError(this ErrorCode code)
        {
            return (int)code > 3000;
        }

        public static void ChecksumMismatch(this ErrorContainer errors, string message)
        {
            errors.AddError(ErrorCode.ChecksumMismatch, default(SourceLocation), $"Checksum mismatch. {message}");
        }

        public static void GenericWarning(this ErrorContainer errors, string message)
        {
            errors.AddError(ErrorCode.GenericWarning, default(SourceLocation), message);
        }

        public static void FormatNotSupported(this ErrorContainer errors, string message)
        {
            errors.AddError(ErrorCode.FormatNotSupported, default(SourceLocation), $"Format is not supported. {message}");
        }

        public static void GenericError(this ErrorContainer errors, string message)
        {
            errors.AddError(ErrorCode.Generic, default(SourceLocation), message);
        }

        public static void InternalError(this ErrorContainer errors, Exception e)
        {
            errors.AddError(ErrorCode.InternalError, default(SourceLocation), $"Internal error. {e.Message}");
        }

        public static void ParseError(this ErrorContainer errors, SourceLocation span, string message)
        {
            errors.AddError(ErrorCode.ParseError, default(SourceLocation), $"Parse error: {message}");
        }
    }
}
