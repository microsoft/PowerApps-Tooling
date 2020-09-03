using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using PAModel;

namespace PASopa
{
    // Mode: Extract 

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"MsApp/Source converter. Version: {SourceSerializer.CurrentSourceVersion}");

            // $$$ MErge in with ADIX PAC
            var mode = args[0].ToLower();
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
                foreach(var msAppPath in Directory.EnumerateFiles(msAppPathDir, "*.msapp", SearchOption.AllDirectories))
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

                MsApp msApp = MsAppSerializer.Load(msAppPath);
                msApp.SaveAsSource(outDir);

                // Test that we can repack 
                {
                    MsApp msApp2 = SourceSerializer.LoadFromSource(outDir);
                    using (var temp = new TempFile())
                    {
                        msApp2.SaveAsMsApp(temp.FullPath);

                        // Will print error on mismatch
                        bool ok = MsAppTest.Compare(msAppPath, temp.FullPath, TextWriter.Null);
                    }
                }
            } else if (mode == "-pack")
            {
                string msAppPath = args[1];
                string outDir = args[2];

                Console.WriteLine($"Pack: {outDir} --> {msAppPath} ");

                MsApp msApp = SourceSerializer.LoadFromSource(msAppPath);
                msApp.SaveAsMsApp(outDir);
            } else
            {
                Console.WriteLine(
@"Usage

-unpack PathToApp.msapp PathToNewSourceFolder
-unpack PathToApp.msapp  // infers source folder
-pack   NewPathToApp.msapp PathToSourceFolder

");
            }
            
        }        
    }
}
