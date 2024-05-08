// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public interface IPaControlInstanceContainer
{
    NamedObjectMapping<ControlGroup> Groups { get; }
    NamedObjectSequence<ControlInstance> Children { get; }
}
