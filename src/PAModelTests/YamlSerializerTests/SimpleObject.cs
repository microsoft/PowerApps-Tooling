// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Yaml;

namespace PAModelTests.YamlSerializerTests;

[YamlObject(Name = "Simple Object")]
internal class SimpleObject
{
    [YamlProperty(Order = 1)]
    public string LastName { get; set; }

    [YamlProperty(Order = 1, Name = "First Name")]
    public string FirstName { get; set; }

    [YamlProperty(Order = 2)]
    public string Description { get; set; }

    [YamlProperty(Order = 2)]
    public string Summary { get; set; }

    [YamlProperty(Order = 2, DefaultValue = "Overview")]
    public string Overview { get; set; } = "Overview";

    [YamlProperty(Order = 3, DefaultValue = 10)]
    public int X { get; set; } = 10;

    [YamlProperty(Order = 3, DefaultValue = 10)]
    public int Y { get; set; } = 10;

    [YamlProperty]
    public int Age { get; set; }

    public decimal Position { get; set; }
}
