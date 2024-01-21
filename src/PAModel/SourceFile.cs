// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IO;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools;

internal enum SourceKind
{
    Control,
    UxComponent,
    DataComponent,
    Test,
    CommandComponent
}

internal class SourceFile
{
    // the source of truth.
    public ControlInfoJson Value { get; set; }

    public SourceKind Kind { get; set; }

    // Convenience accessors.
    public string ControlName => Value.TopParent.Name;
    public string ControlId => Value.TopParent.ControlUniqueId;

    // For a data component, this is a guid. Very important.
    public string TemplateName => Value.TopParent.Template.Name;

    internal string GetMsAppFilename()
    {
        if (Kind == SourceKind.Control)
        {
            return $"Controls\\{ControlId}.json";
        }
        else if (Kind == SourceKind.DataComponent || Kind == SourceKind.UxComponent || Kind == SourceKind.CommandComponent)
        {
            return $"Components\\{ControlId}.json";
        }
        else if (Kind == SourceKind.Test)
        {
            return $"AppTests\\{ControlId}.json";
        }
        throw new NotImplementedException($"Unrecognized source kind:" + Kind);
    }

    private IEnumerable<ControlInfoJson.Item> Flatten(ControlInfoJson.Item control)
    {
        return control.Children?.Concat(control.Children.SelectMany(Flatten)) ?? Enumerable.Empty<ControlInfoJson.Item>();
    }

    public IEnumerable<ControlInfoJson.Item> Flatten()
    {
        return (new ControlInfoJson.Item[] { Value.TopParent }).Concat(Flatten(Value.TopParent));
    }

    public FileEntry ToMsAppFile()
    {
        var file = MsAppSerializer.ToFile(FileKind.Unknown, Value);
        file.Name = FilePath.FromMsAppPath(GetMsAppFilename());

        return file;
    }

    public static SourceFile New(ControlInfoJson json)
    {
        var sf = new SourceFile
        {
            Value = json
        };

        var x = sf.Value.TopParent;
        if (x.Template.Id == ControlInfoJson.Template.DataComponentId)
        {
            sf.Kind = SourceKind.DataComponent;
        }
        else if (x.Template.Id == ControlInfoJson.Template.UxComponentId)
        {
            sf.Kind = SourceKind.UxComponent;
        }
        else if (x.Name == SourceSerializer.AppTestControlName)
        {
            sf.Kind = SourceKind.Test;
        }
        else if (x.Template.Id == ControlInfoJson.Template.CommandComponentId)
        {
            sf.Kind = SourceKind.CommandComponent;
        }
        else
        {
            // UX Control has many different Template ids.
            sf.Kind = SourceKind.Control;
        }

        return sf;
    }
}
