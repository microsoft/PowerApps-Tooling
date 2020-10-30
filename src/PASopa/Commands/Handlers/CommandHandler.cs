using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace PASopa.Commands.Handlers
{
    public class CommandHandler
    {
        private readonly ILogger<CommandHandler> _logger;

        protected CommandHandler(ILogger<CommandHandler> logger)
        {
            _logger = logger;
        }

        protected bool IsValidInput()
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
