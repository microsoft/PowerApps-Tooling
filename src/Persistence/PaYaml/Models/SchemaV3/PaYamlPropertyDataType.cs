// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public enum PaYamlPropertyDataType
{
    /// <summary>
    /// Indicates an data type which has either not been explicitly set or has been determined to be invalid.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// aka Void. Only valid for function return types which are allowed to have side effects.
    /// </summary>
    None,
    Text,
    Number,
    Boolean,
    DateAndTime,
    Screen,
    Record,
    Table,
    Image,
    VideoOrAudio,
    Color,
    Currency,
}
