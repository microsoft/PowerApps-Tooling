// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public interface IControlTemplateStore
{
    void Add(ControlTemplate controlTemplate);

    bool TryGetTemplateByName(string name, [MaybeNullWhen(false)] out ControlTemplate controlTemplate);

    bool TryGetControlTypeByName(string name, [MaybeNullWhen(false)] out Type controlType);

    ControlTemplate GetByName(string name);

    bool TryGetById(string id, [MaybeNullWhen(false)] out ControlTemplate controlTemplate);

    bool TryGetByType(Type type, [MaybeNullWhen(false)] out ControlTemplate controlTemplate);

    ControlTemplate GetById(string id);

    bool Contains(Type type);

    bool Contains(string name);

    bool TryGetName(Type type, [MaybeNullWhen(false)] out string name);

    Type GetControlType(string name);
}
