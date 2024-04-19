// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

/// <summary>
/// The set of error codes representing kinds of errors which may or may not indicate whether the error is actionable by the user.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "For internal use only.")]
public enum PersistenceErrorCode
{
    // 1xxx - System errors
    SystemError = 1000,

    // 2xxx - Serialization errors
    SerializationError = 2000,
    InvalidObjectGraph = 2001,

    // 3xxx - Deserialization errors
    DeserializationError = 3000,
    YamlInvalidSyntax = 3001,
    YamlInvalidSchema = 3101,
    EditorStateJsonEmptyOrNull = 3102,
    InvalidEditorStateJson = 3300,
    ControlInstanceInvalid = 3501,
    RoundTripValidationFailed = 3502,

    //
    MsappArchiveError = 5000,

    [EditorBrowsable(EditorBrowsableState.Never)]
    _LastErrorExclusive,
}

public static class PersistenceErrorCodeExtensions
{
    internal static PersistenceErrorCode CheckArgumentInRange(this PersistenceErrorCode errorCode)
    {
        if (errorCode < PersistenceErrorCode.SystemError || errorCode >= PersistenceErrorCode._LastErrorExclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode), "The error code is out of range.");
        }

        return errorCode;
    }

    internal static string? GetDefaultExceptionMessage(this PersistenceErrorCode errorCode)
    {
        CheckArgumentInRange(errorCode);

        // note: Please make sure the message returned is a complete sentence that ends with a period.
        // The 'PersistenceException.Reason' will get appended to it as additional information.
        switch (errorCode)
        {
            case PersistenceErrorCode.SystemError:
                return "An error occurred in the persistence library.";
            case PersistenceErrorCode.SerializationError:
                return "An error occurred during serialization.";
            case PersistenceErrorCode.InvalidObjectGraph:
                return "An invalid object graph was detected.";
            case PersistenceErrorCode.DeserializationError:
                return "An error occurred during deserialization.";
            case PersistenceErrorCode.YamlInvalidSyntax:
                return "Invalid YAML syntax was encountered during deserialization.";
            case PersistenceErrorCode.YamlInvalidSchema:
                return "The YAML does not comply with the supported schema.";
            case PersistenceErrorCode.EditorStateJsonEmptyOrNull:
                return "An editor state json file was empty or had a null value.";
            case PersistenceErrorCode.InvalidEditorStateJson:
                return "An editor state json file could not be deserialized due to being invalid.";
            case PersistenceErrorCode.ControlInstanceInvalid:
                return "A control instance object in YAML has an invalid state.";
            case PersistenceErrorCode.RoundTripValidationFailed:
                return "Round trip yaml validation failed.";
            case PersistenceErrorCode.MsappArchiveError:
                return "An error was detected in an msapp file.";
            default:
                return "An exception occurred in the persistence library.";
        }
    }

    /// <summary>
    /// Returns the general error code group for simpler classification handling.
    /// </summary>
    public static PersistenceErrorCode? GetErrorCodeGroup(this PersistenceErrorCode errorCode)
    {
        if (errorCode < PersistenceErrorCode.SystemError)
        {
            return null;
        }
        else if (errorCode < PersistenceErrorCode.SerializationError)
        {
            return PersistenceErrorCode.SystemError;
        }
        else if (errorCode < PersistenceErrorCode.DeserializationError)
        {
            return PersistenceErrorCode.SerializationError;
        }
        else if (errorCode < PersistenceErrorCode.DeserializationError)
        {
            return PersistenceErrorCode.SerializationError;
        }
        else if (errorCode < PersistenceErrorCode.MsappArchiveError)
        {
            return PersistenceErrorCode.DeserializationError;
        }
        else if (errorCode < PersistenceErrorCode._LastErrorExclusive)
        {
            return PersistenceErrorCode.MsappArchiveError;
        }
        else
        {
            return null;
        }
    }
}
