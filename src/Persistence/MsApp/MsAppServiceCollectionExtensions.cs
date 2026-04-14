// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

public static class MsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMsappArchiveFactory"/> service with default implementation of <see cref="MsappArchiveFactory"/>.
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    public static void AddMsappArchiveFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IMsappArchiveFactory, MsappArchiveFactory>();
    }
}
