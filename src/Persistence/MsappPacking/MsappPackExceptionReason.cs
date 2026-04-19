// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Identifies the scenario in which an <see cref="MsappPackException"/> was thrown.
/// </summary>
public enum MsappPackExceptionReason
{
    /// <summary>
    /// An output file already exists and overwriting output is not enabled.
    /// </summary>
    OutputAlreadyExists,
}
