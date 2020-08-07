using System;
using System.Dynamic;
using System.Text.Json;
using PAModel;

namespace PASopa
{
    // Mode: Extract 

    class Program
    {    
        static void Main(string[] args)
        {
            // $$$ MErge in with ADIX PAC
            var mode = args[0].ToLower();
            if (mode =="-test")
            {
                string msAppPath = args[1];
                Console.WriteLine("Test roundtripping: " + msAppPath);

                // Test round-tripping 
                MsAppTest.StressTest(msAppPath);
            } else if (mode == "-unpack")
            {
                string msAppPath = args[1];
                string outDir = args[2];

                Console.WriteLine($"Unpack: {msAppPath} --> {outDir} ");

                MsApp msApp = MsAppSerializer.Load(msAppPath);
                msApp.SaveAsSource(outDir);
            } else if (mode == "-pack")
            {
                string msAppPath = args[1];
                string outDir = args[2];

                Console.WriteLine($"Pack: {outDir} --> {msAppPath} ");

                MsApp msApp = SourceSerializer.LoadFromSource(msAppPath);
                msApp.SaveAsMsApp(outDir);
            } else
            {
                Console.WriteLine("Unrecognized mode");
            }
            
        }        
    }
}
