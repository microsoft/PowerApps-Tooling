using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PASopa.Commands.Handlers
{
    public class MakeHandler : CommandHandler, ICommandHandler
    {
        private readonly ILogger<MakeHandler> _logger;

        [Required]
        public string MsAppPath { get; set; }

        [Required]
        public string PackagesPath { get; set; }

        [Required]
        public string InputApp { get; set; }

        public MakeHandler(ILogger<MakeHandler> logger) : base(logger)
        {
            _logger = logger;
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            return Task.FromResult(Run());
        }

        private int Run()
        {
            return !IsValidInput() ? 0 : Execute();
        }

        protected int Execute()
        {
            _logger.LogInformation($"Pack: {InputApp} --> {MsAppPath} ");
            var appName = Path.GetFileName(MsAppPath);
            var app = CanvasDocument.MakeFromSources(appName, PackagesPath, new List<string>() { InputApp });
            app.SaveAsMsApp(MsAppPath);
            return 1;
        }
    }
}
