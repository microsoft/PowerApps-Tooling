// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Backdoor.Repl.Menus;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl.Functions
{
    public interface IResult<T>
    {
        public string Message { get; }
        public IEnumerable<IError> Errors { get; }
        public T Context { get; }
    }

    public class ResultState<T> : IResult<T>
    {
        public ResultState(T context, IEnumerable<IError> errors = default(IEnumerable<IError>), string message = default(string))
        {
            Message = message;
            Errors = errors;
            Context = context;
        }

        public string Message { get; }
        public IEnumerable<IError> Errors { get; }
        public T Context { get; }
    }

    public interface IMenuResultState<T> : IResult<T>
    {
        public IMenu<T> Menu { get; }
    }

    public class MenuResultState<T> : ResultState<T>, IMenuResultState<T>
    {
        public static IMenuResultState<T> FromResultState(IResult<T> result, IMenu<T> menu) =>
            new MenuResultState<T>(menu, result.Context, result.Errors, result.Message);

        public IMenu<T> Menu { get; }

        public MenuResultState(IMenu<T> menu, T context, IEnumerable<IError> errors = default(IEnumerable<IError>), string message = default(string))
            : base(context, errors, message)
        {
            Menu = menu;
        }
    }
}
