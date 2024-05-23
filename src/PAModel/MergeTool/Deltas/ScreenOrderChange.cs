// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Extensions;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class ScreenOrderChange(List<string> screenOrder) : IDelta
{
    public void Apply(CanvasDocument document)
    {
        // Clone this, we don't want to potentially modify the order from one of the loaded CanvasDocuments
        document._screenOrder = screenOrder.JsonClone();
    }
}
