// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// The reason why a path is invalid for usage in a <see cref="PaArchive"/>.
/// To get an exception friendly message, use <see cref="PaArchivePath.GetInvalidReasonExceptionMessage"/>.
/// </summary>
public enum PaArchivePathInvalidReason
{
    InvalidPathChars,
    WhitespaceOnlySegment,
    SegmentWithLeadingOrTrailingWhitespace,
    IllegalSegment,
}
