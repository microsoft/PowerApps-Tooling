using System.CommandLine;

namespace PASopa.Commands
{
    public class UnPack: Command
    {
        private const string CommandDescription = "Extract canvas app for scm and code reviews";

        public UnPack(string name) : base(name, CommandDescription)
        {
            AddOption(new Option<int>(new[]
            {
                "--ms-app-path"
            }, "Canvas app path"));
            AddOption(new Option<int>(new[]
            {
                "--output-directory"
            }, "Output directory path"));
        }
    }
}
