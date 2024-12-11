// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// Defines the required implementation for creating a strong-typed string based on a pre-validated value.<br/>
/// It is recommended to use 'explicit implementation' for this interface as these methods are not intended to be called directly.
/// </summary>
/// <typeparam name="TSelf">The type that implements this interface.</typeparam>
public interface ITypedStringCreator<TSelf>
    where TSelf : ITypedStringCreator<TSelf>, ITypedStringValidator
{
    static abstract TSelf CreateFromValid(ValidatedString<TSelf> validatedValue);
}
