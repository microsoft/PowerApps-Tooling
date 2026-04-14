// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

public static class MsappPackingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="MsappPackingService"/> service and its dependencies.
    /// </summary>
    public static void AddMsappPackingService(this IServiceCollection services)
    {
        services.TryAddSingleton<MsappPackingService>();
        // And register dependencies
        services.AddMsappArchiveFactory();
        services.TryAddSingleton<MsappReferenceArchiveFactory>();
    }
}
