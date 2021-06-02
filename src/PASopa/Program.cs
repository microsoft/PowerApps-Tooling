// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PASopa
{
    // Mode: Extract

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"MsApp/Source converter. Version: {SourceSerializer.CurrentSourceVersion}");

            var mode = args.Length > 0 ? args[0]?.ToLower() : null;
            if (mode == "-test")
            {
                if (args.Length < 2)
                {
                    Usage();
                    return;
                }

                string msAppPath = args[1];
                Console.WriteLine("Test roundtripping: " + msAppPath);

                // Test round-tripping
                MsAppTest.StressTest(msAppPath);
                return;
            }
            if (mode == "-testall")
            {
                if (args.Length < 2)
                {
                    Usage();
                    return;
                }
                // Roundtrip all .msapps in a folder.
                string msAppPathDir = args[1];
                int countTotal = 0;
                int countPass = 0;
                Console.WriteLine("Test roundtripping all .msapps in : " + msAppPathDir);
                foreach (var msAppPath in Directory.EnumerateFiles(msAppPathDir, "*.msapp", SearchOption.TopDirectoryOnly))
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    bool ok = MsAppTest.StressTest(msAppPath);
                    var str = ok ? "Pass" : "FAIL";
                    countTotal++;
                    if (ok) { countPass++; }
                    sw.Stop();
                    Console.WriteLine($"Test: {Path.GetFileName(msAppPath)}: {str}  ({sw.ElapsedMilliseconds / 1000}s)");
                }
                Console.WriteLine($"{countPass}/{countTotal}  ({countPass * 100 / countTotal}% passed");
            }
            else if (mode == "-unpack")
            {
                if (args.Length < 2)
                {
                    Usage();
                    return;
                }

                string msAppPath = args[1];
                msAppPath = Path.GetFullPath(msAppPath);

                if (!msAppPath.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("must be path to .msapp file");
                }

                string outDir;
                if (args.Length == 2)
                {
                    outDir = msAppPath.Substring(0, msAppPath.Length - 6) + "_src"; // chop off ".msapp";
                }
                else
                {
                    outDir = args[2];
                }

                Console.WriteLine($"Unpack: {msAppPath} --> {outDir} ");

                (CanvasDocument msApp, ErrorContainer errors) = TryOperation(() => CanvasDocument.LoadFromMsapp(msAppPath));
                errors.Write(Console.Error);

                if (errors.HasErrors)
                {
                    return;
                }

                errors = TryOperation(() => msApp.SaveToSources(outDir));
                errors.Write(Console.Error);
                if (errors.HasErrors)
                {
                    return;
                }

                // Test that we can repack
                {
                    (CanvasDocument msApp2, ErrorContainer errors2) = TryOperation(() => CanvasDocument.LoadFromSources(outDir));
                    errors2.Write(Console.Error);
                    if (errors2.HasErrors)
                    {
                        return;
                    }

                    using (var temp = new TempFile())
                    {
                        errors2 = TryOperation(() => msApp2.SaveToMsAppValidation(temp.FullPath));
                        errors2.Write(Console.Error);
                        if (errors2.HasErrors)
                        {
                            return;
                        }

                        bool ok = MsAppTest.Compare(msAppPath, temp.FullPath, TextWriter.Null);
                    }
                }
            }
            else if (mode == "-pack")
            {
                if (args.Length < 3)
                {
                    Usage();
                    return;
                }

                string msAppPath = Path.GetFullPath(args[1]);
                string inputDir = Path.GetFullPath(args[2]);

                Console.WriteLine($"Pack: {inputDir} --> {msAppPath} ");

                (CanvasDocument msApp, ErrorContainer errors) = TryOperation(() => CanvasDocument.LoadFromSources(inputDir));
                errors.Write(Console.Error);
                if (errors.HasErrors)
                {
                    return;
                }
                errors = TryOperation(() => msApp.SaveToMsApp(msAppPath));
                errors.Write(Console.Error);
                if (errors.HasErrors)
                {
                    return;
                }
            }
            else if (mode == "-make")
            {
                if (args.Length < 4)
                {
                    Usage();
                    return;
                }

                string msAppPath = Path.GetFullPath(args[1]);
                string pkgsPath = Path.GetFullPath(args[2]);
                string inputPA = Path.GetFullPath(args[3]);

                Console.WriteLine($"Make: {inputPA} --> {msAppPath} ");

                var appName = Path.GetFileName(msAppPath);

                (var app, var errors) = TryOperation(() => CanvasDocument.MakeFromSources(appName, pkgsPath, new List<string>() { inputPA }));
                errors.Write(Console.Error);
                if (errors.HasErrors)
                {
                    return;
                }
                errors = TryOperation(() => app.SaveToMsApp(msAppPath));
                errors.Write(Console.Error);
                if (errors.HasErrors)
                {
                    return;
                }
            }
            else if (mode == "-merge")
            {
                if (args.Length < 5)
                {
                    Usage();
                    return;
                }

                string path1 = args[1];
                string path2 = args[2];
                string parent = args[3];
                string pathresult = args[4];

                Console.WriteLine($"Merge is very experimental right now, do not rely on this behavior");
                Console.WriteLine($"Merge: {path1}, {path2} --> {pathresult} ");


                (var app1, var errors1) = TryOperation(() => CanvasDocument.LoadFromSources(path1));
                errors1.Write(Console.Error);
                if (errors1.HasErrors)
                {
                    return;
                }

                (var app2, var errors2) = TryOperation(() => CanvasDocument.LoadFromSources(path2));
                errors2.Write(Console.Error);
                if (errors2.HasErrors)
                {
                    return;
                }

                (var parentApp, var errors3) = TryOperation(() => CanvasDocument.LoadFromSources(parent));
                errors3.Write(Console.Error);
                if (errors3.HasErrors)
                {
                    return;
                }

                var result = CanvasMerger.Merge(app1, app2, parentApp);

                var errors = result.SaveToSources(pathresult);
                errors.Write(Console.Error);
                if (errors.HasErrors)
                {
                    return;
                }
            }
            else
            {
                Usage();
            }
        }

        private static void Usage()
        {
            Console.WriteLine(
                @"Usage

                -unpack PathToApp.msapp PathToNewSourceFolder
                -unpack PathToApp.msapp  // infers source folder
                -pack  NewPathToApp.msapp PathToSourceFolder
                -make PathToCreateApp.msapp PathToPkgFolder PathToPaFile
                -merge path1 path2 parentPath resultpath

                ");
        }

        private static (CanvasDocument, ErrorContainer) TryOperation(Func<(CanvasDocument, ErrorContainer)> operation)
        {
            CanvasDocument app = null;
            ErrorContainer errors = new ErrorContainer();
            try
            {
                (app, errors) = operation();
            }
            catch (Exception e)
            {
                // Add unhandled exception to the error container.
                errors.InternalError(e);
            }
            return (app, errors);
        }

        private static ErrorContainer TryOperation(Func<ErrorContainer> operation)
        {
            ErrorContainer errors = new ErrorContainer();
            try
            {
                errors = operation();
            }
            catch (Exception e)
            {
                // Add unhandled exception to the error container.
                errors.InternalError(e);
            }
            return errors;
        }
    }
}
