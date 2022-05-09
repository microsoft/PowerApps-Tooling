// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Backdoor.Repl.Menus;

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
                    string output;
                    (current, output) = current.TransferFunction(input, subject, out var errors);
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"Error:  {error}");
                    }

                    if (output != default(string))
                        Console.WriteLine($"\n{output}");
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
