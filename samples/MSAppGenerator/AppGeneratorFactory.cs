// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace MSAppGenerator;

/// <summary>
/// MSApp generator factory
/// </summary>
public class AppGeneratorFactory : IAppGeneratorFactory
{
    private readonly IControlFactory _controlFactory;

    public AppGeneratorFactory(IControlFactory controlFactory)
    {
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(_controlFactory));
    }

    /// <summary>
    /// Instantiates and returns the requested type of generator
    /// </summary>
    public IAppGenerator Create(bool interactive)
    {
        return interactive
            ? new InteractiveGenerator(_controlFactory)
            : new ExampleGenerator(_controlFactory);
    }
}
