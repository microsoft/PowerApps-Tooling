// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3_0;

public static class ModelsExtensions
{
    public static IEnumerable<NamedObject<ControlInstance>> DescendantControlInstances<TContainer>(this NamedObject<TContainer> namedContainer)
        where TContainer : IPaControlInstanceContainer
    {
        _ = namedContainer ?? throw new ArgumentNullException(nameof(namedContainer));

        return namedContainer.Value.DescendantControlInstances();
    }

    public static IEnumerable<NamedObject<ControlInstance>> DescendantControlInstances(this IPaControlInstanceContainer container)
    {
        _ = container ?? throw new ArgumentNullException(nameof(container));

        // Preorder Traverse of tree using a loop
        var stack = new Stack<NamedObject<ControlInstance>>();

        // Load up with top-level children first
        foreach (var child in container.Children.Reverse())
        {
            stack.Push(child);
        }

        while (stack.Count != 0)
        {
            var topNamedObject = stack.Pop();
            foreach (var child in topNamedObject.Value.Children.Reverse())
            {
                stack.Push(child);
            }
            yield return topNamedObject;
        }
    }

    public static IEnumerable<string> SelectNames<TValue>(this IEnumerable<NamedObject<TValue>> namedObjects)
        where TValue : notnull
    {
        return namedObjects.Select(o => o.Name);
    }
}
