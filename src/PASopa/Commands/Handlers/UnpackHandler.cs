using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PASopa.Commands.Handlers
{
    public class UnpackHandler : CommandHandler
    {
        private readonly ILogger<CommandHandler> _logger;
        public string MsAppPath { get; set; }
        public string OutputDirectory { get; set; }

        public UnpackHandler(ILogger<CommandHandler> logger) : base(logger)
        {
            _logger = logger;
        }

        protected override int Execute()
        {
            string appPath = Path.GetFullPath(MsAppPath);

            if (!appPath.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("must be path to .msapp file");
            }

            string outDir = OutputDirectory;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = appPath.Substring(0, appPath.Length - 6) + "_src"; // chop off ".msapp";
            }

            _logger.LogInformation($"Unpack: {appPath} --> {outDir} ");

            CanvasDocument msApp = MsAppSerializer.Load(appPath);
            msApp.SaveAsSource(outDir);

            CanvasDocument msApp2 = SourceSerializer.LoadFromSource(outDir);
            TempFile temp = new TempFile();
            msApp2.SaveAsMsApp(temp.FullPath);

            bool ok = MsAppTest.Compare(appPath, temp.FullPath, TextWriter.Null);
            return ok ? 0 : 1;
        }
    }
}
