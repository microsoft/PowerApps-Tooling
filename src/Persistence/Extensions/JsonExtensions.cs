// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class JsonExtensions
{
    /// <summary>
    /// A fluent way of calling <see cref="JsonSerializerOptions.MakeReadOnly()"/> to make a <see cref="JsonSerializerOptions"/> instance immutable.
    /// Especially useful for shared static instances.
    /// </summary>
    public static JsonSerializerOptions MakeReadOnlyFluent(this JsonSerializerOptions options)
    {
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
