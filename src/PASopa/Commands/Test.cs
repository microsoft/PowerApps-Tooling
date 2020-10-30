using System.CommandLine;

namespace PASopa.Commands
{
    public class Test : Command
    {
        private const string CommandDescription = "Test canvas app(s) in this project";

        public Test(string name, string description = null) : base(name, CommandDescription)
        {
            AddOption(new Option<int>(new[]
            {
                "--ms-app-path"
            }, "Canvas app path or file location"));

            AddOption(new Option<int>(new[]
            {
                "--all"
            }, "Test all canvas apps in the path"));
        }
    }
}
