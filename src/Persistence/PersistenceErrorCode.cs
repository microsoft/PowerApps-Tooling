// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
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
    DuplicateNameInSequence = 3503,

    //
    PaArchiveError = 5000,
    PaArchiveMissingRequiredEntry = 5001,
    PaArchiveEntryDeserializedToJsonNull = 5002,
    PaArchiveEntryDeserializedToJsonFailed = 5003,
    ZipArchiveBlockedDueToInvalidEntryPathCharsOnNetFramework = 5004,

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
        return errorCode switch
        {
            PersistenceErrorCode.SystemError => "An error occurred in the persistence library.",
            PersistenceErrorCode.SerializationError => "An error occurred during serialization.",
            PersistenceErrorCode.InvalidObjectGraph => "An invalid object graph was detected.",
            PersistenceErrorCode.DeserializationError => "An error occurred during deserialization.",
            PersistenceErrorCode.YamlInvalidSyntax => "Invalid YAML syntax was encountered during deserialization.",
            PersistenceErrorCode.YamlInvalidSchema => "The YAML does not comply with the supported schema.",
            PersistenceErrorCode.EditorStateJsonEmptyOrNull => "An editor state json file was empty or had a null value.",
            PersistenceErrorCode.InvalidEditorStateJson => "An editor state json file could not be deserialized due to being invalid.",
            PersistenceErrorCode.ControlInstanceInvalid => "A control instance object in YAML has an invalid state.",
            PersistenceErrorCode.RoundTripValidationFailed => "Round trip yaml validation failed.",
            PersistenceErrorCode.DuplicateNameInSequence => "An item name was duplicated.",
            PersistenceErrorCode.PaArchiveError => "An error was detected in a Power Apps archive file.",
            PersistenceErrorCode._LastErrorExclusive => throw new InvalidOperationException("The error code is out of range."),
            _ => "An exception occurred in the persistence library.",
        };
    }
}
