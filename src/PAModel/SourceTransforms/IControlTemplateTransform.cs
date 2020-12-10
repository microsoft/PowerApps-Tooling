// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    /// <summary>
    /// Interface for changing controls based on the template
    /// AfterParse must be symmetric with BeforeWrite
    /// They are applied bottom-up, so any child controls will have been already transformed by the time these are called
    /// </summary>
    interface IControlTemplateTransform
    {
        IEnumerable<string> TargetTemplates { get; }
        void BeforeWrite(BlockNode control);
        void AfterRead(BlockNode control);
    }
}
