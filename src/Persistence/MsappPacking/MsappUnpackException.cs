// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Exception thrown when unpacking an msapp fails.
/// </summary>
public sealed class MsappUnpackException : Exception
{
    public MsappUnpackException(string message)
        : base(message)
    {
    }

    public MsappUnpackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
