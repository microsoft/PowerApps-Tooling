// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// A strong-typed string that is guaranteed to not be empty or whitespace only (i.e. <see cref="string.IsNullOrWhiteSpace"/>).
/// </summary>
[TypedString]
public sealed partial record NonWhitespaceString
{
    private static bool IsValid([NotNullWhen(true)] string? value) => !string.IsNullOrWhiteSpace(value);
}
