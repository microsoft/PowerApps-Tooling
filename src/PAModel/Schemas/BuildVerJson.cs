// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class BuildVerJson
    {
        public string CommitHash { get; set; }
        public bool IsLocalBuild { get; set; }
    }
}
