// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NETFRAMEWORK || NETSTANDARD2_0
#pragma warning disable

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

// C# 9 init property setters and record types do not quite function out-of-the-box
// for net4.8, but do so long as an "System.Runtime.CompilerServices.IsExternalInit"
// type merely exists.
[EditorBrowsable(EditorBrowsableState.Never)]
#if TFMADAPTERS_PUBLIC
public
#else
internal
#endif
static class IsExternalInit
{
}

#pragma warning restore
#endif
