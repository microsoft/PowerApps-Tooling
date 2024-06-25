// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Primitives;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

namespace Persistence.Tests.Extensions;

[DebuggerNonUserCode]
public static class PaYamlLocationAssertionExtensions
{
    public static PaYamlLocationAssertions Should(this PaYamlLocation? actualValue)
    {
        return new(actualValue);
    }
}

[DebuggerNonUserCode]
public class PaYamlLocationAssertions(PaYamlLocation? actualValue)
    : ObjectAssertions<PaYamlLocation?, PaYamlLocationAssertions>(actualValue)
{
}
