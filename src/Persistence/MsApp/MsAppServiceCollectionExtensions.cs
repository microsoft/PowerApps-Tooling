// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

public static class MsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMsappArchiveFactory"/> service with default implementation of <see cref="MsappArchiveFactory"/>.
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    /// <param name="entryNameEncoding">See parameter of same name on <see cref="ZipArchive(Stream, ZipArchiveMode, bool, Encoding?)"/>.</param>
    public static void AddMsappArchiveFactory(this IServiceCollection services, Encoding? entryNameEncoding = null)
    {
        services.AddSingleton<IMsappArchiveFactory>(sp =>
        {
            if (entryNameEncoding is null)
            {
                return MsappArchiveFactory.Default;
            }
            else
            {
                return new MsappArchiveFactory() { EntryNameEncoding = entryNameEncoding };
            }
        });
    }
}
