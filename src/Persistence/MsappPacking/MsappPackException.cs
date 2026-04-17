// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Exception thrown when packing an msapp fails.
/// </summary>
public sealed class MsappPackException : Exception
{
    public MsappPackException(MsappPackExceptionReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    public MsappPackException(MsappPackExceptionReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>
    /// Identifies the reason why this exception was thrown.
    /// </summary>
    public MsappPackExceptionReason Reason { get; }
}
