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
    MsappArchive Create(string path, bool overwrite = false);

    /// <summary>
    /// Creates a new msapp archive with the provided stream.
    /// </summary>
    MsappArchive Create(Stream stream, bool leaveOpen = false);

    /// <summary>
    /// Opens existing msapp archive for read.
    /// </summary>
    MsappArchive Open(string path);

    /// <summary>
    /// Opens existing msapp archive for read.
    /// </summary>
    MsappArchive Open(Stream stream, bool leaveOpen = false);

    /// <summary>
    /// Opens existing msapp archive for update (read/write).
    /// </summary>
    MsappArchive Update(string path);

    /// <summary>
    /// Opens existing msapp archive for update (read/write).
    /// </summary>
    MsappArchive Update(Stream stream, bool leaveOpen = false);
}
