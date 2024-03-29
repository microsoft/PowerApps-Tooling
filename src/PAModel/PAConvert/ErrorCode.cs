// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace Microsoft.PowerPlatform.Formulas.Tools;

/// <summary>
/// Error codes from reading, writing (compiling) a document.
/// These numbers must stay stable. 
/// </summary>
internal enum ErrorCode
{
    // Warnings start at 2000
    ChecksumMismatch = 2001,

    // Missing properties or values where they're required
    ValidationWarning = 2002,

    // Something in the Yaml file won't round-trip. 
    YamlWontRoundtrip = 2010,

    // Catch-all - review and remove. 
    GenericWarning = 2999,

    // Errors start at 3000
    InternalError = 3001,

    FormatNotSupported = 3002,

    // Parse error 
    ParseError = 3003,

    // This operation isn't supported, use studio
    UnsupportedUseStudio = 3004,

    // Missing properties or values where they're required
    ValidationError = 3005,

    // Editor State file is corrupt. Delete it. 
    IllegalEditorState = 3006,

    // Sanity check on the final msapp fails.
    // This is a generic error. There's nothing the user can do here (it's the msapp)
    // We should have caught it sooner and issues a more actionable source-level error. 
    MsAppError = 3007,

    // A symbol is already defined.
    // Common for duplicate controls. This suggests an offline edit. 
    DuplicateSymbol = 3008,

    // Msapp is corrupt. Can't read it. 
    CantReadMsApp = 3010,

    // Post-unpack validation failed, this always indicates a bug on our side
    UnpackValidationFailed = 3011,

    // Bad parameter (such as a missing file)
    BadParameter = 3012,

    // JSON Property's Value was Changed, Doesn't Match
    JSONValueChanged = 3013,

    // JSON Property was added
    JSONPropertyAdded = 3014,

    // JSON Property was removed
    JSONPropertyRemoved = 3015,

    // Catch-all.  Should review and make these more specific. 
    Generic = 3999,

    // Some of the cases which the tool doesn't support, see below example:
    // Connection Accounts is using the old CDS connector which is incompatable with this tool
    UnsupportedError = 4001

}

internal static class ErrorCodeExtensions
{
    public static bool IsError(this ErrorCode code)
    {
        return (int)code > 3000;
    }

    public static void ChecksumMismatch(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.ChecksumMismatch, default, $"Checksum mismatch. {message}");
    }

    public static void ValidationWarning(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.ValidationWarning, default, $"Validation issue: {message}");
    }

    public static void ValidationWarning(this ErrorContainer errors, SourceLocation span, string message)
    {
        errors.AddError(ErrorCode.ValidationWarning, span, $"Validation issue {message}");
    }

    public static void PostUnpackValidationFailed(this ErrorContainer errors)
    {
        errors.AddError(ErrorCode.UnpackValidationFailed, default, "Roundtrip validation on unpack failed. \nYou have found a bug; this is not a specific bug, rather an indicator that some bug has been encountered.\nPlease open an issue and log the entirety of this error log at https://github.com/microsoft/PowerApps-Language-Tooling\n");
    }

    public static void YamlWontRoundTrip(this ErrorContainer errors, SourceLocation loc, string message)
    {
        errors.AddError(ErrorCode.YamlWontRoundtrip, loc, message);
    }

    public static void GenericWarning(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.GenericWarning, default, message);
    }

    public static void FormatNotSupported(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.FormatNotSupported, default, $"Format is not supported. {message}");
    }

    public static void GenericError(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.Generic, default, message);
    }

    public static void InternalError(this ErrorContainer errors, Exception e)
    {
        errors.AddError(ErrorCode.InternalError, default, $"Internal error. {e.Message}\r\nStack Trace:\r\n{e.StackTrace}");
    }

    public static void UnsupportedOperationError(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.UnsupportedUseStudio, default, $"Unsupported operation error. {message}");
    }

    public static void EditorStateError(this ErrorContainer errors, SourceLocation loc, string message)
    {
        errors.AddError(ErrorCode.IllegalEditorState, loc, $"Illegal editorstate file. {message}");
    }

    public static void GenericMsAppError(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.MsAppError, default, $"MsApp is corrupted. {message}");
    }

    public static void DuplicateSymbolError(this ErrorContainer errors, SourceLocation loc, string message, SourceLocation loc2)
    {
        errors.AddError(ErrorCode.DuplicateSymbol, loc, $"Symbol '{message}' is already defined. Previously at {loc2}");
    }

    public static void ParseError(this ErrorContainer errors, SourceLocation span, string message)
    {
        errors.AddError(ErrorCode.ParseError, span, $"Parse error: {message}");
    }

    public static void ValidationError(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.ValidationError, default, $"Validation error: {message}");
    }

    public static void ValidationError(this ErrorContainer errors, SourceLocation span, string message)
    {
        errors.AddError(ErrorCode.ValidationError, span, $"Validation error: {message}");
    }

    public static void MsAppFormatError(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.CantReadMsApp, default, $"MsApp is corrupted: {message}");
    }

    public static void JSONValueChanged(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.JSONValueChanged, default, $"Property Value Changed: {message}");
    }

    public static void JSONPropertyAdded(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.JSONPropertyAdded, default, $"Property Added: {message}");
    }

    public static void JSONPropertyRemoved(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.JSONPropertyRemoved, default, $"Property Removed: {message}");
    }
    public static void UnsupportedError(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.UnsupportedError, default, $"Not Supported: {message}");
    }

    public static void BadParameter(this ErrorContainer errors, string message)
    {
        errors.AddError(ErrorCode.BadParameter, default, $"Bad parameter: {message}");
    }
}
