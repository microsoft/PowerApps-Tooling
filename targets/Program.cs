// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.Json;
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
            
            string RootDir = "", gitHash = "";
            bool gitExists = true;
            try 
            {
                RootDir = Read("git", "rev-parse --show-toplevel", noEcho: true).Trim();
                gitHash = Read("git", "rev-parse HEAD", noEcho: true).Trim();               
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
            string SrcDir = Path.Combine(RootDir, "src");

            string LogDir = Path.Combine(ObjDir, "logs");
            string TestLogDir = Path.Combine(ObjDir, "testLogs");

            string PAModelDir = Path.Combine(SrcDir, "PAModel");
            var solution = Path.Combine(SrcDir, "PASopa.sln");
            
            var project = Path.Combine(PAModelDir, "Microsoft.PowerPlatform.Formulas.Tools.csproj");

            Target("squeaky-clean",
                () =>
                {
                    CleanDirectory(BinDir);
                    CleanDirectory(ObjDir);
                    CleanDirectory(PkgDir);
                });

            Target("clean",
                () => RunDotnet("clean", $"{EscapePath(solution)} --configuration {options.Configuration}", gitExists, LogDir));

            Target("restore",
                DependsOn("clean"),
                () => RunDotnet("restore", $"{EscapePath(solution)}", gitExists, LogDir));

            Target("build",
                () => {
                    if (gitExists)
                        CreateBuildHashFile(ObjDir, gitHash);
                    RunDotnet("build", $"{EscapePath(solution)} --configuration {options.Configuration} --no-restore", gitExists, LogDir);
                });

            Target("test",
                () => RunDotnet("test", $"{EscapePath(solution)} --configuration {options.Configuration} --no-build --logger trx --results-directory {EscapePath(TestLogDir)}", gitExists, LogDir));

            Target("rebuild",
                DependsOn("restore", "build"));

            Target("pack",
                () => RunDotnet("pack", $"{EscapePath(project)} --configuration {options.Configuration} --output {EscapePath(Path.Combine(PkgDir, "PackResult"))} --no-build -p:Packing=true", gitExists, LogDir));

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
            var optionsLogPath = Path.Combine(LogDir, $"{verb}-{options.Configuration}");
            var logSettings = $"/clp:verbosity=minimal /flp:Verbosity=normal;LogFile={EscapePath(optionsLogPath + ".log")} /flp3:PerformanceSummary;Verbosity=diag;LogFile={EscapePath(optionsLogPath + ".diagnostics.log")}";
            Run("dotnet", $"{verb} {verbArgs} {logSettings} {gitDef} /nologo");
        }

        static void CreateBuildHashFile(string objDir, string gitHash)
        {
            var filePath = Path.Combine(objDir, "buildver.json");
            var file = new System.IO.FileInfo(filePath);
            file.Directory.Create();

            var jsonContents = new {
                CommitHash = gitHash,
#if !ADOBuild
                IsLocalBuild = true
#endif
            };
            var text = JsonSerializer.Serialize(jsonContents);
            File.WriteAllText(filePath, text);
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
        }

        static string EscapePath(string path)
        {
            return $"\"{path}\"";
        }
    }
}
