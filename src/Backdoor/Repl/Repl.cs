// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Backdoor.Repl.Menus;
using Microsoft.PowerPlatform.Formulas.Tools.PAConvert;

namespace Backdoor.Repl
{
    public class Repl<T>
    {
        public static void Start(IMenu<T> mainMenu, T subject)
        {
            var current = mainMenu;

            while (true) {
                Console.WriteLine(current.Title);
                Console.Write(current.Description);
                try
                {
                    var input = Console.ReadLine();
                    var result = current.TransferFunction(input, subject);

                    if (result.Errors != default(IEnumerable<IError>))
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error:  {error}");
                        }
                    }

                    if (result.Message != default(string))
                        Console.WriteLine($"\n{result.Message}");

                    subject = result.Context;
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
