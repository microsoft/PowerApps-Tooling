using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PASopa.Commands.Handlers
{
    public class TestHandler: CommandHandler
    {
        private readonly ILogger<CommandHandler> _logger;
        public string MsAppPath { get; set; }
        public string All { get; set; }

        public TestHandler(ILogger<CommandHandler> logger) : base(logger)
        {
            _logger = logger;
        }

        protected override int Execute()
        {
            if (All == "all")
            {
                TestMultiple(MsAppPath);
                return 0;
            }

            TestSingle(MsAppPath);
            return 0;
        }

        private void TestSingle(string appPath)
        {
            _logger.LogInformation($"Test rountripping:{appPath}");
            MsAppTest.StressTest(appPath);
        }

        private void TestMultiple(string appPath)
        {
            _logger.LogInformation("Test roundtripping all .msapps in : " + appPath);
            var countTotal = 0;
            int countPass = 0;
            foreach(var msAppPath in Directory.EnumerateFiles(appPath, "*.msapp", SearchOption.TopDirectoryOnly))
            {
                Stopwatch sw = Stopwatch.StartNew();
                bool ok = MsAppTest.StressTest(msAppPath);
                var str = ok ? "Pass" : "FAIL";
                countTotal++;
                if (ok) { countPass++; }
                sw.Stop();
                Console.Out.WriteLine($"Test: {Path.GetFileName(msAppPath)}: {str}  ({sw.ElapsedMilliseconds/1000}s)");
            }
            Console.Out.WriteLine($"{countPass}/{countTotal}  ({countPass * 100 / countTotal}% passed");
        }

    }
}
