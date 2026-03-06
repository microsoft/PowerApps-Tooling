// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Exception thrown when packing an msapp fails.
/// </summary>
public sealed class MsappPackException : Exception
{
    public MsappPackException(string message)
        : base(message)
    {
    }

    public MsappPackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
