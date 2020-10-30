using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PASopa.Commands.Handlers
{
    public class MakeHandler : CommandHandler
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

        protected override int Execute()
        {
            _logger.LogInformation($"Pack: {InputApp} --> {MsAppPath} ");
            var appName = Path.GetFileName(MsAppPath);
            var app = CanvasDocument.MakeFromSources(appName, PackagesPath, new List<string>() { InputApp });
            app.SaveAsMsApp(MsAppPath);
            return 1;
        }
    }
}
