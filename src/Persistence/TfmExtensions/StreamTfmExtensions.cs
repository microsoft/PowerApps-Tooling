// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Globalization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmExtensions;

public static class StreamTfmExtensions
{
#if NETFRAMEWORK
    private const int DefaultCopyBufferSize = 81920;

    public static Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken)
    {
        return source.CopyToAsync(destination, bufferSize: DefaultCopyBufferSize, cancellationToken);
    }
#endif
}
