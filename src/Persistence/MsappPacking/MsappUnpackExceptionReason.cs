// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Identifies the scenario in which an <see cref="MsappUnpackException"/> was thrown.
/// </summary>
public enum MsappUnpackExceptionReason
{
    /// <summary>
    /// An output file or folder already exists and overwriting output is not enabled.
    /// </summary>
    OutputAlreadyExists,

    /// <summary>
    /// The MSApp structure version is below the minimum supported version.
    /// </summary>
    UnsupportedMSAppStructureVersion,

    /// <summary>
    /// The document version is below the minimum supported version.
    /// </summary>
    UnsupportedDocVersion,
}
