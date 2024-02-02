// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public interface IControlTemplateStore
{
    void Add(ControlTemplate controlTemplate);

    bool TryGetByName(string name, [NotNullWhen(true)] out ControlTemplate? controlTemplate);

    ControlTemplate GetByName(string name);

    bool TryGetByUri(string uri, [NotNullWhen(true)] out ControlTemplate? controlTemplate);

    ControlTemplate GetByUri(string uri);
}
