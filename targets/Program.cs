using System;
using System.IO;
using System.Runtime;
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
        static string RootDir = Read("git", "rev-parse --show-toplevel", noEcho: true).Trim();
        static string BinDir = Path.Combine(RootDir, "bin");
        static string ObjDir = Path.Combine(RootDir, "obj");
        static string PkgDir = Path.Combine(RootDir, "pkg");
        static string LogDir = Path.Combine(ObjDir, "logs");
        static string TestLogDir = Path.Combine(ObjDir, "testLogs");
        static Options options;

        static void Main(string[] args)
        {
            var solution = Path.Combine(RootDir, "src/PASoPa.sln");
            var project = Path.Combine(RootDir, "src/PAModel/Microsoft.PowerPlatform.Formulas.Tools.csproj");

            Target("squeaky-clean",
                () =>
                {
                    CleanDirectory(BinDir);
                    CleanDirectory(ObjDir);
                    CleanDirectory(PkgDir);
                });

            Target("clean",
                () => RunDotnet("clean", $"{solution} --configuration {options.Configuration}"));

            Target("restore",
                DependsOn("clean"),
                () => RunDotnet("restore", $"{solution}"));

            Target("build",
                () => RunDotnet("build", $"{solution} --configuration {options.Configuration} --no-restore"));

            Target("test",
                () => RunDotnet("test", $"{solution} --configuration {options.Configuration} --no-build --logger trx --results-directory {TestLogDir}"));

            Target("rebuild",
                DependsOn("restore", "build"));

            Target("pack",
                () => RunDotnet("pack", $" {project} --configuration {options.Configuration} --output {Path.Combine(PkgDir, "PackResult")} --no-build -p:Packing=true"));

            Target("ci",
                DependsOn("squeaky-clean", "rebuild", "test"));

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
                {
                    options = o;
                    try{
                        RunTargetsWithoutExiting(new[] {options.Target},
                            logPrefix: options.Target,
                            messageOnly: ex => ex is NonZeroExitCodeException);
                    }
                    catch 
                    {
                        Environment.Exit(1);
                    }
                })
            .WithNotParsed(errs =>
            {
                RunTargetsAndExit(args);
            });
        }

        static void RunDotnet(string verb, string verbArgs)
        {
            var logSettings = $"/clp:verbosity=minimal /flp:Verbosity=normal;LogFile={LogDir}/{verb}-{options.Configuration}.log /flp3:PerformanceSummary;Verbosity=diag;LogFile={LogDir}/{verb}-{options.Configuration}.diagnostics.log";
            Run("dotnet", $"{verb} {verbArgs} {logSettings} /nologo");
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
