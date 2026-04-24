// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmExtensions;

public static class ReflectionTfmExtensions
{
#if !NET5_0_OR_GREATER
    public static bool IsAssignableTo(this Type type, [NotNullWhen(true)] Type? targetType) => targetType?.IsAssignableFrom(type) ?? false;
#endif
}
