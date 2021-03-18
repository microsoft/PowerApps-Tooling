// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal interface IDelta
    {
        void Apply(CanvasDocument document);
    }
}
