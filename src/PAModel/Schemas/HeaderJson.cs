// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AppMagic.Authoring.Persistence;

internal class HeaderJson
{
    public Version DocVersion { get; set; }
    public Version MinVersionToLoad { get; set; }
    public Version MSAppStructureVersion { get; set; }
    public DateTime? LastSavedDateTimeUTC { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    public static HeaderJson CreateDefault()
    {
        var defaultHeader = new HeaderJson
        {
            // combine doc/min versions, they aren't used separately anymore
            DocVersion = new Version("1.294"),
            MinVersionToLoad = new Version("1.294"),

            MSAppStructureVersion = new Version("2.0")
        };

        return defaultHeader;
    }
}
