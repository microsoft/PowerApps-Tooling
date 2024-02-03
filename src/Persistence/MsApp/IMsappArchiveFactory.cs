// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Msapp archive factory.
/// </summary>
public interface IMsappArchiveFactory
{
    /// <summary>
    /// Creates a new msapp archive.
    /// </summary>
    IMsappArchive Create(string path);

    /// <summary>
    /// Creates a new msapp archive with the provided stream.
    /// </summary>
    IMsappArchive Create(Stream stream, bool leaveOpen = false);

    /// <summary>
    /// Opens existing msapp archive.
    /// </summary>
    IMsappArchive Open(string path);

    /// <summary>
    /// Opens existing msapp archive.
    /// </summary>
    IMsappArchive Open(Stream stream, bool leaveOpen = false);
}
