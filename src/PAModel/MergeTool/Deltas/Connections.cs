// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class AddConnection : IDelta
{
    public string Name;
    public ConnectionJson Contents;

    public void Apply(CanvasDocument document)
    {
        document._connections ??= new Dictionary<string, ConnectionJson>(StringComparer.Ordinal);

        if (document._connections.ContainsKey(Name))
            return;

        document._connections.Add(Name, Contents);
    }
}
internal class RemoveConnection : IDelta
{
    public string Name;

    public void Apply(CanvasDocument document)
    {
        if (!(document._connections?.ContainsKey(Name) ?? false))
            return;

        document._connections.Remove(Name);
    }
}
