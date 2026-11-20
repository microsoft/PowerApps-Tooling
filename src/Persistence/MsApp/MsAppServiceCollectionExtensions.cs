// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

public static class MsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMsappArchiveFactory"/> service with default implementation of <see cref="MsappArchiveFactory"/>.
    /// Archives are opened without limits.
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    public static void AddMsappArchiveFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IMsappArchiveFactory, MsappArchiveFactory>();
    }

    /// <summary>
    /// Registers the <see cref="IMsappArchiveFactory"/> service with an implementation of <see cref="MsappArchiveFactory"/> which
    /// checks the given limits when existing archives are opened.
    /// Replaces any existing <see cref="IMsappArchiveFactory"/> registration (e.g. the one without limits added by
    /// <c>AddMsappPackingService()</c>), so the limits apply regardless of registration order.
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    /// <param name="limits">The limits to check. See <see cref="MsappArchiveFactory.Limits"/>.</param>
    public static void AddMsappArchiveFactory(this IServiceCollection services, PaArchiveLimits limits)
    {
        ThrowIfNull(limits);
        services.Replace(ServiceDescriptor.Singleton<IMsappArchiveFactory>(sp => new MsappArchiveFactory(sp.GetService<ILogger<MsappArchive>>()) { Limits = limits }));
    }
}
