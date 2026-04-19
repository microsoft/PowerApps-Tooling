// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Exception thrown when unpacking an msapp fails.
/// </summary>
public sealed class MsappUnpackException : Exception
{
    public MsappUnpackException(MsappUnpackExceptionReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    public MsappUnpackException(MsappUnpackExceptionReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    /// <summary>
    /// Identifies the reason why this exception was thrown.
    /// </summary>
    public MsappUnpackExceptionReason Reason { get; }
}
