// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal interface IDelta
{
    void Apply(CanvasDocument document);
}
