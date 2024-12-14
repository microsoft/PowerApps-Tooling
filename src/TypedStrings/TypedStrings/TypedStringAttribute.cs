// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// Instructs the TypedStrings source generator to generate an implementation for a strong-typed string.<br/>
/// You may specify a custom `IsValid` method to explicitly validate a string has a specific format with the minimum
/// requirement that the value must not be a null.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TypedStringAttribute : Attribute
{
}
