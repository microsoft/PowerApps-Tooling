// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Parser
{
    internal enum TokenKind
    {
        // Miscellaneous
        None,
        Eof,
        Indent,
        Dedent,
        PAExpression,

        Identifier,

        // Punctuators
        PropertyStart,
        TemplateSeparator,
        VariantSeparator,

        // Keywords
        Control,
        Component,
    }
}
