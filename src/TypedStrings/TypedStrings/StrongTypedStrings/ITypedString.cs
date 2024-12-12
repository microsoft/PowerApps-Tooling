// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.TypedStrings.StrongTypedStrings;

/// <summary>
/// Represents a strong-typed string value which is guaranteed to be valid; removing the need to check.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public interface ITypedString<TSelf> : IParsable<TSelf>
    where TSelf : ITypedString<TSelf>
{
    /// <summary>
    /// Gets the validated value of this typed string instance.
    /// </summary>
    string Value { get; }

    static abstract TSelf Parse(string value);

    static abstract bool TryParse([NotNullWhen(true)] string? value, [MaybeNullWhen(false)] out TSelf result);
}
