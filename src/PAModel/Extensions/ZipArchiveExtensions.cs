// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools.Extensions;

public static class ZipArchiveExtensions
{
    public static byte[] ToBytes(this ZipArchiveEntry e)
    {
        using (var s = e.Open())
        {
            // Non-seekable stream.
            var buffer = new byte[e.Length];
            var bytesRead = 0;

            do
            {
                bytesRead += s.Read(buffer, bytesRead, (int)e.Length - bytesRead);
            } while (bytesRead < e.Length);


            return buffer;
        }
    }

    // JsonElement is loss-less, handles unknown fields without dropping them.
    // Converting to a Poco drops fields we don't recognize.
    public static JsonElement ToJson(this ZipArchiveEntry e)
    {
        using (var s = e.Open())
        {
            var doc = JsonDocument.Parse(s);
            return doc.RootElement;
        }
    }
}
