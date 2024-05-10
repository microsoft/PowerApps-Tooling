// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3_0;

public interface IPaControlInstanceContainer
{
    NamedObjectMapping<ControlGroup> Groups { get; }
    NamedObjectSequence<ControlInstance> Children { get; }
}
