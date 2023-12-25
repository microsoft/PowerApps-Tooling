// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

[Flags]
public enum YamlLexerOptions
{
    None = 0,
    EnforceLeadingEquals = 1
}
