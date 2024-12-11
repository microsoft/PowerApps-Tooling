// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// Implements the core validation logic for a typed string.
/// </summary>
public interface ITypedStringValidator
{
    /// <summary>
    /// Validates a potential value for the typed string.
    /// The default implementation simply allows any non-null value, which helps simplify the most common case of just wanting a strong-typed
    /// <see cref="string"/>.
    /// </summary>
    /// <returns>A value indicating whether <paramref name="value"/> is valid.</returns>
    static abstract bool IsValid([NotNullWhen(true)] string? value);
}
