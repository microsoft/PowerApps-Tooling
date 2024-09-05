// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public interface IPaControlInstanceContainer
{
    NamedObjectSequence<ControlInstance>? Children { get; }
}
