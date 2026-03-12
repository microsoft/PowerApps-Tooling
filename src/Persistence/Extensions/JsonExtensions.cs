// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class JsonExtensions
{
    /// <summary>
    /// A fluent way of making a <see cref="JsonSerializerOptions"/> instance immutable.
    /// Especially useful for shared static instances.
    /// </summary>
    public static JsonSerializerOptions ToReadOnly(this JsonSerializerOptions options)
    {
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
