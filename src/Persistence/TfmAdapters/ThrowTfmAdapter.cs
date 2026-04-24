// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmAdapters;

/// <summary>
/// Guard-clause helpers that provide a uniform call pattern across all target frameworks.
/// On .NET 6+/.NET 8+, the BCL equivalents (ArgumentNullException.ThrowIfNull,
/// ArgumentException.ThrowIfNullOrEmpty, ArgumentException.ThrowIfNullOrWhiteSpace) are
/// available but require qualified syntax.  Using this class lets all TFMs — including net48 —
/// share the same unqualified call sites via the project-level global using static import.
/// </summary>
public static class ThrowTfmAdapter
{
    /// <summary>Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    public static void ThrowIfNull(
        [NotNull] object? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(argument, paramName);
#else
        if (argument is null)
            throw new ArgumentNullException(paramName);
#endif
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null,
    /// or <see cref="ArgumentException"/> if it is empty.
    /// </summary>
    public static void ThrowIfNullOrEmpty(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
#else
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (argument.Length == 0)
            throw new ArgumentException("The value cannot be null or empty.", paramName);
#endif
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null,
    /// or <see cref="ArgumentException"/> if it is empty or white-space only.
    /// </summary>
    public static void ThrowIfNullOrWhiteSpace(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (argument.Trim().Length == 0)
            throw new ArgumentException("The value cannot be null or white space.", paramName);
#endif
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if <paramref name="condition"/> is true.
    /// </summary>
    public static void ThrowObjectDisposedIf([DoesNotReturnIf(true)] bool condition, object instance)
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(condition, instance);
#else
        if (condition)
            throw new ObjectDisposedException(instance.GetType().FullName);
#endif
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if <paramref name="condition"/> is true.
    /// </summary>
    public static void ThrowObjectDisposedIf([DoesNotReturnIf(true)] bool condition, Type type)
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(condition, type);
#else
        if (condition)
            throw new ObjectDisposedException(type.FullName);
#endif
    }
}
