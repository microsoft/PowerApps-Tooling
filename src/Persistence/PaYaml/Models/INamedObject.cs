// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

public interface INamedObject<TName, TValue>
    where TName : notnull
    where TValue : notnull
{
    TName Name { get; }
    TValue Value { get; }
    YamlLocation? Start { get; }
}
