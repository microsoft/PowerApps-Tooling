// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// An interface for objects that can be checked for whether it can be considered 'empty', namely that a non-null instance may have no properties which are not considered empty.<br/>
/// Often used in serialization to determine whether a property could be treated as a 'null'.
/// </summary>
public interface ISupportsIsEmpty
{
    bool IsEmpty();
}

public static class SupportsIsEmptyExtensions
{
    public static T? EmptyToNull<T>(this T? instance)
        where T : notnull, ISupportsIsEmpty
    {
        return instance is null || instance.IsEmpty() ? default : instance;
    }
}
