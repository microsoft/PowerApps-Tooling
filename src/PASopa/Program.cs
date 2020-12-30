// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
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
            if (mode =="-test")
            {
                string msAppPath = args[1];
                Console.WriteLine("Test roundtripping: " + msAppPath);

                // Test round-tripping
                MsAppTest.StressTest(msAppPath);
                return;
            }
            if (mode == "-testall")
            {
                // Roundtrip all .msapps in a folder.
                string msAppPathDir = args[1];
                int countTotal = 0;
                int countPass = 0;
                Console.WriteLine("Test roundtripping all .msapps in : " + msAppPathDir);
                foreach(var msAppPath in Directory.EnumerateFiles(msAppPathDir, "*.msapp", SearchOption.TopDirectoryOnly))
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    bool ok = MsAppTest.StressTest(msAppPath);
                    var str = ok ? "Pass" : "FAIL";
                    countTotal++;
                    if (ok) { countPass++; }
                    sw.Stop();
                    Console.WriteLine($"Test: {Path.GetFileName(msAppPath)}: {str}  ({sw.ElapsedMilliseconds/1000}s)");
                }
                Console.WriteLine($"{countPass}/{countTotal}  ({countPass * 100 / countTotal}% passed");
            }
            else if (mode == "-unpack")
            {
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

                (CanvasDocument msApp, ErrorContainer errors) = CanvasDocument.LoadFromMsapp(msAppPath);
                errors.Write(Console.Out);

                if (errors.HasErrors)
                {
                    return;
                }

                errors = msApp.SaveToSources(outDir);
                errors.Write(Console.Out);
                if (errors.HasErrors)
                {
                    return;
                }

                // Test that we can repack
                {
                    (CanvasDocument msApp2, ErrorContainer errors2) = CanvasDocument.LoadFromSources(outDir);
                    errors2.Write(Console.Out);
                    if (errors2.HasErrors)
                    {
                        return;
                    }

                    using (var temp = new TempFile())
                    {
                        errors2 = msApp2.SaveToMsApp(temp.FullPath);
                        errors.Write(Console.Out);
                        if (errors.HasErrors)
                        {
                            return;
                        }

                        bool ok = MsAppTest.Compare(msAppPath, temp.FullPath, TextWriter.Null);
                    }
                }
            }
            else if (mode == "-pack")
            {
                string msAppPath = args[1];
                string inputDir = args[2];

                Console.WriteLine($"Pack: {inputDir} --> {msAppPath} ");

                (CanvasDocument msApp, ErrorContainer errors) =  CanvasDocument.LoadFromSources(inputDir);
                errors.Write(Console.Out);
                if (errors.HasErrors)
                {
                    return;
                }
                errors = msApp.SaveToMsApp(msAppPath);
                errors.Write(Console.Out);
                if (errors.HasErrors)
                {
                    return;
                }
            }
            else if (mode == "-make")
            {
                string msAppPath = args[1];
                string pkgsPath = args[2];
                string inputPA = args[3];

                Console.WriteLine($"Make: {inputPA} --> {msAppPath} ");

                var appName = Path.GetFileName(msAppPath);

                (var app, var errors) = CanvasDocument.MakeFromSources(appName, pkgsPath, new List<string>() { inputPA });
                errors.Write(Console.Out);
                if (errors.HasErrors)
                {
                    return;
                }
                errors = app.SaveToMsApp(msAppPath);
                errors.Write(Console.Out);
                if (errors.HasErrors)
                {
                    return;
                }
            }
            else
            {
                Console.WriteLine(
@"Usage

-unpack PathToApp.msapp PathToNewSourceFolder
-unpack PathToApp.msapp  // infers source folder
-pack  NewPathToApp.msapp PathToSourceFolder
-make PathToCreateApp.msapp PathToPkgFolder PathToPaFile

");
            }
        }
    }
}
