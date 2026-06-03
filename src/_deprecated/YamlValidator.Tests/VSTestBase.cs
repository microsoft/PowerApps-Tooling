// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Persistence.YamlValidator.Tests;

/// <summary>
/// Represents a shared base class for any test class that uses the Visual Studio Test Framework.
/// </summary>
/// <remarks>
/// DO NOT add any setup/tear down logic to this class, as not all tests may require it.
/// The preferred approach is to use a different derived base class for tests that require setup/tear down logic specific to some shared scenarios.
/// </remarks>
public abstract class VSTestBase
{
    public required TestContext TestContext { get; set; }
}
