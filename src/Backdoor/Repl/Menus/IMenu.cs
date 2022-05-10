// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Backdoor.Repl.Functions;

namespace Backdoor.Repl.Menus
{
    public interface IMenu<T>
    {
        public IMenuResultState<T> TransferFunction(string input, T document);
        public string Title { get; }
        public string Description { get; }
    }
}
