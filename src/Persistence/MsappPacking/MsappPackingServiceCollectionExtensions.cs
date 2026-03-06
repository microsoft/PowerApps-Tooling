// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

public static class MsappPackingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="MsappReferenceArchiveFactory"/> service.
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    public static void AddMsappReferenceArchiveFactory(this IServiceCollection services)
    {
        services.AddSingleton<MsappReferenceArchiveFactory>();
    }
}
