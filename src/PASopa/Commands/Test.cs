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
                "-map",
                "/map",
                "--ms-app-path"
            }, "Canvas app path"));
            AddArgument(new Argument<string>("all"));
        }
    }
}
