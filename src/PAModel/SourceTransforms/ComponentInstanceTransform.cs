// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;

internal class ComponentInstanceTransform : IControlTemplateTransform
{
    // Key is name in source, value is name in target
    // For AfterRead, that's ComponentID => ComponentName
    // For BeforeWrite, that's ComponentName => ComponentID
    internal Dictionary<string, string> ComponentRenames = new Dictionary<string, string>();
    private ErrorContainer _errors;

    public ComponentInstanceTransform(ErrorContainer errors)
    {
        _errors = errors;
    }


    public IEnumerable<string> TargetTemplates => ComponentRenames.Keys;

    public void AfterRead(BlockNode control)
    {
        DoRename(control);
    }

    public void BeforeWrite(BlockNode control)
    {
        DoRename(control);
    }

    private void DoRename(BlockNode control)
    {
        var templateName = control.Name?.Kind?.TypeName ?? string.Empty;
        if (ComponentRenames.TryGetValue(templateName, out var rename))
        {
            control.Name.Kind.TypeName = rename;
        }
        else
        {
            _errors.ValidationWarning("Renaming component instance but unable to find target name");
        }
    }
}
