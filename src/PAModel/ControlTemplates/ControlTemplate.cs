//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;


namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    public sealed class ControlTemplate
    {
        private readonly string _id;
        private string _version;
        private string _name;
        private readonly Dictionary<string, string> _inputDefaults;
    }
}
