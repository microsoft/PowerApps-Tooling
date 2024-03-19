// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public IAppGenerator Create(bool interactive)
    {
        return interactive
            ? new InteractiveGenerator(_controlFactory)
            : new ExampleGenerator(_controlFactory);
    }
}
