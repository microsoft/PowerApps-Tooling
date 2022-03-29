// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    // Corresponds with https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/data-types
    // plus the addition of Screen, which is available in the component property type picker.
    internal enum PropertyDataType
    {
    Invalid,
    _Min,
    Record,
    Table,
    Boolean,
    Number,
    String,
    Date,
    Time,
    DateTime,
    DateTimeNoTimeZone,
    Hyperlink,
    Currency,
    Image,
    Color,
    Enum,
    Media,
    Guid,
    Screen,
    _Lim
    }
}
