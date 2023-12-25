// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class AddTemplate : IDelta
{
    private string _name;
    private CombinedTemplateState _template;
    private TemplatesJson.TemplateJson _jsonTemplate;
    private PcfControl _pcfTemplate;

    private bool _isPcf;

    public AddTemplate(string name, CombinedTemplateState templateState, TemplatesJson.TemplateJson jsonTemplate)
    {
        _name = name;
        _template = templateState;
        _jsonTemplate = jsonTemplate;
        _isPcf = false;
    }

    public AddTemplate(string name, CombinedTemplateState templateState, PcfControl pcfTemplate)
    {
        _name = name;
        _template = templateState;
        _pcfTemplate = pcfTemplate;
        _isPcf = true;
    }

    public void Apply(CanvasDocument document)
    {
        if (document._templateStore.AddTemplate(_name, _template))
        {
            if (_isPcf && _pcfTemplate != null)
            {
                document._pcfControls.Add(_name, _pcfTemplate);
            }
            else if (_jsonTemplate != null)
            {
                document._templates.UsedTemplates = document._templates.UsedTemplates.Concat(new[] { _jsonTemplate }).ToArray();
            }
        }
    }
}
