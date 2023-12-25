// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class ScreenOrderChange : IDelta
{
    private readonly List<string> _screenOrder;

    public ScreenOrderChange(List<string> screenOrder)
    {
        _screenOrder = screenOrder;
    }

    public void Apply(CanvasDocument document)
    {
        // Clone this, we don't want to potentially modify the order from one of the loaded CanvasDocuments
        document._screenOrder = _screenOrder.JsonClone();
    }
}
