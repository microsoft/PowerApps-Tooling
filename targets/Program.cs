using System;
using System.IO;
using CommandLine;
using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace targets
{
    class Options
    {
        [Value(0, MetaName = "target", HelpText = "build target to run; see available with: '--list", Default = "rebuild")]
        public string Target { get; set; }

        [Option('c', "configuration", Required = false, Default = "Debug")]
        public string Configuration { get; set; }
    }

    class Program
    {
        static Options options;

        static void Main(string[] args)
        {
            
            string RootDir = "";
            bool gitExists = true;
            try 
            {
                RootDir = Read("git", "rev-parse --show-toplevel", noEcho: true).Trim(); 
            }
            catch
            {
                RootDir = Directory.GetCurrentDirectory();
                Console.WriteLine("Unable to find root directory using git, assuming this script is being run from root = " + RootDir);
                gitExists = false;
            }

            string BinDir = Path.Combine(RootDir, "bin");
            string ObjDir = Path.Combine(RootDir, "obj");
            string PkgDir = Path.Combine(RootDir, "pkg");
            string LogDir = Path.Combine(ObjDir, "logs");
            string TestLogDir = Path.Combine(ObjDir, "testLogs");

            var solution = Path.Combine(RootDir, "src/PASopa.sln");
            var project = Path.Combine(RootDir, "src/PAModel/Microsoft.PowerPlatform.Formulas.Tools.csproj");

            Target("squeaky-clean",
                () =>
                {
                    CleanDirectory(BinDir);
                    CleanDirectory(ObjDir);
                    CleanDirectory(PkgDir);
                });

            Target("clean",
                () => RunDotnet("clean", $"{solution} --configuration {options.Configuration}", gitExists, LogDir));

            Target("restore",
                DependsOn("clean"),
                () => RunDotnet("restore", $"{solution}", gitExists, LogDir));

            Target("build",
                () => RunDotnet("build", $"{solution} --configuration {options.Configuration} --no-restore", gitExists, LogDir));

            Target("test",
                () => RunDotnet("test", $"{solution} --configuration {options.Configuration} --no-build --logger trx --results-directory {TestLogDir}", gitExists, LogDir));

            Target("rebuild",
                DependsOn("restore", "build"));

            Target("pack",
                () => RunDotnet("pack", $" {project} --configuration {options.Configuration} --output {Path.Combine(PkgDir, "PackResult")} --no-build -p:Packing=true", gitExists, LogDir));

            Target("ci",
                DependsOn("squeaky-clean", "rebuild", "test"));

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
                {
                    options = o;
                    RunTargetsAndExit(new[] {options.Target},
                        logPrefix: options.Target,
                        messageOnly: ex => ex is NonZeroExitCodeException);
                })
            .WithNotParsed(errs =>
            {
                RunTargetsAndExit(args);
            });
        }

        static void RunDotnet(string verb, string verbArgs, bool gitExists, string LogDir)
        {
            var gitDef = "";
            if (gitExists) 
                gitDef = "-p:GitExists=true";
            var logSettings = $"/clp:verbosity=minimal /flp:Verbosity=normal;LogFile={LogDir}/{verb}-{options.Configuration}.log /flp3:PerformanceSummary;Verbosity=diag;LogFile={LogDir}/{verb}-{options.Configuration}.diagnostics.log";
            Run("dotnet", $"{verb} {verbArgs} {logSettings} {gitDef} /nologo");
        }

        static void CleanDirectory(string directoryPath)
        {
            directoryPath = Path.GetFullPath(directoryPath);
            Console.WriteLine($"Cleaning directory: {directoryPath}");
            try {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
            catch (AccessViolationException) { /* swallow */ }
            // catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
