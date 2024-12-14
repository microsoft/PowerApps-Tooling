// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// A strong-typed string that is guaranteed to not be empty (i.e. <see cref="string.Empty"/>).
/// </summary>
[TypedString]
public sealed partial record NonEmptyString
{
    private static bool IsValid([NotNullWhen(true)] string? value) => !string.IsNullOrEmpty(value);
}
