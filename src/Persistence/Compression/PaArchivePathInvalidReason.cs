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
    /// <summary>
    /// Relative segments like "." or ".." are not allowed, as they can lead to confusion and potential security issues when extracting archives.
    /// </summary>
    RelativeSegment,
    /// <summary>
    /// On Windows, filenames cannot end with ".". So we exclude on all platforms.
    /// </summary>
    SegmentEndsWithDot,
}
