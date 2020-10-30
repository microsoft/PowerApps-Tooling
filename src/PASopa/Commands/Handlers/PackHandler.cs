using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PASopa.Commands.Handlers
{
    public class PackHandler: CommandHandler
    {
        private readonly ILogger<CommandHandler> _logger;

        [Required]
        public string MsAppPath { get; set; }

        [Required]
        public string InputDirectory { get; set; }

        public PackHandler(ILogger<CommandHandler> logger) : base(logger)
        {
            _logger = logger;
        }

        protected override int Execute()
        {
            _logger.LogInformation($"Pack: {InputDirectory} --> {MsAppPath}");
            CanvasDocument msApp = SourceSerializer.LoadFromSource(InputDirectory);
            msApp.SaveAsMsApp(MsAppPath);
            return 0;
        }
    }
}
