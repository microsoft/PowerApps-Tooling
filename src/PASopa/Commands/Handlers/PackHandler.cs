using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PASopa.Commands.Handlers
{
    public class PackHandler: CommandHandler, ICommandHandler
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
            _logger.LogInformation($"Pack: {InputDirectory} --> {MsAppPath}");
            CanvasDocument msApp = SourceSerializer.LoadFromSource(InputDirectory);
            msApp.SaveAsMsApp(MsAppPath);
            return 0;
        }
    }
}
