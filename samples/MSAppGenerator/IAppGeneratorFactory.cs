// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSAppGenerator;

/// <summary>
/// Interface for msapp generator factory
/// </summary>
public interface IAppGeneratorFactory
{
    /// <summary>
    /// Creates a MSApp generator
    /// </summary>
    IAppGenerator Create(bool interactive);
}
