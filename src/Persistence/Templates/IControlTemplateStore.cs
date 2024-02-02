// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public interface IControlTemplateStore
{
    void Add(ControlTemplate controlTemplate);

    bool TryGetControlTemplateByUri(string uri, [NotNullWhen(true)] out ControlTemplate? controlTemplate);

    bool TryGetControlTemplateByName(string name, [NotNullWhen(true)] out ControlTemplate? controlTemplate);
}