using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PASopa.Commands.Handlers
{
    public abstract class CommandHandler : ICommandHandler
    {
        private readonly ILogger<CommandHandler> _logger;

        protected CommandHandler(ILogger<CommandHandler> logger)
        {
            _logger = logger;
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            return Task.FromResult(Run());
        }

        protected abstract int Execute();

        private int Run()
        {
            return !IsValidInput() ? 0 : Execute();
        }

        private bool IsValidInput()
        {
            ValidationContext context = new ValidationContext(this, null, null);
            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool valid = Validator.TryValidateObject(this, context, validationResults, true);

            if (valid)
            {
                return true;
            }

            foreach (ValidationResult validationResult in validationResults)
            {
                _logger.LogError($"{validationResult.ErrorMessage}");
            }

            return false;

        }
    }
}
