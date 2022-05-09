// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    public interface ICanvasDocument
    {
        public bool TryRemoveControl(string controlName, out IEnumerable<IError> errors);
        public bool Exists(string controlName);
        public IEnumerable<string> GetChildren(string controlName);
        public IEnumerable<IError> SaveToFile(string fullPathToMsApp);
        public IEnumerable<IError> SaveToSource(string pathToSourceDirectory);
        public IEnumerable<string> Screens { get; }
    }
}
