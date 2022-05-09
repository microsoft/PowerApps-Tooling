// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal class DeleteVisitorContext
    {
        public DeleteVisitorContext(string nameToDelete, Action<BlockNode> deleteNode)
        {
            NameToDelete = nameToDelete;
            DeleteNode = deleteNode;
        }

        public Action<BlockNode> DeleteNode { get; set; }
        public string NameToDelete { get; }
        public bool DidDelete { get; set; }
    }
}
