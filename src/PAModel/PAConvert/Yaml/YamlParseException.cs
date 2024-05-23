// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

public class YamlParseException(string message, int line = 0, Exception innerException = null)
    : Exception(message, innerException)
{
    public int Line { get; init; } = line;
}
