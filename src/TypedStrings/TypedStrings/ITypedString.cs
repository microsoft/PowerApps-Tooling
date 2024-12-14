// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// Represents a strong-typed string value which is guaranteed to be valid; removing the need to check.
/// </summary>
/// <typeparam name="TSelf">The concrete type of the typed string.</typeparam>
public interface ITypedString<TSelf> : IParsable<TSelf>, IComparable<TSelf>
    where TSelf : ITypedString<TSelf>
{
    /// <summary>
    /// Gets the validated string value of this typed string instance.
    /// This value is guaranteed to never be null.
    /// </summary>
    string Value { get; }
}
