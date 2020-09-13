using System;
using System.IO;
using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace targets
{
    class Program
    {
        static string rootDir = Read("git", "rev-parse --show-toplevel", noEcho: true).Trim();
        static void Main(string[] args)
        {
            var pkgDir = Path.Combine(rootDir, "pkg");
            var logDir = Path.Combine(rootDir, "obj", "logs");
            var testLogDir = Path.Combine(rootDir, "obj", "testLogs");
            var logSettings = $"/clp:verbosity=minimal /flp:Verbosity=normal;LogFile={logDir}/msbuild.log /flp3:PerformanceSummary;Verbosity=diag;LogFile={logDir}/msbuild.diagnostics.log";

            var solution = Path.Combine(rootDir, "src/PASoPa.sln");

            Target("clean",
                () => Run("dotnet", $"clean {solution} {logSettings} /nologo"));

            Target("restore",
                DependsOn("clean"),
                () => Run("dotnet", $"restore {solution} {logSettings} /nologo"));

            Target("build",
                () => Run("dotnet", $"build {solution} {logSettings} /nologo"));

            Target("test",
                () => Run("dotnet", $"test {solution} --no-build --logger trx --results-directory {testLogDir} {logSettings} /nologo"));

            Target("rebuild",
                DependsOn("restore", "build"));

            Target("pack",
                () => Run("dotnet", $"pack --output {pkgDir} --no-build {solution} {logSettings} /nologo"));

            Target("ci",
                DependsOn("rebuild", "test", "pack"));

            Target("default", DependsOn("rebuild"));

             RunTargetsAndExit(args, messageOnly: ex => ex is NonZeroExitCodeException);
        }
    }
}
