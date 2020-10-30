using System.CommandLine;

namespace PASopa.Commands
{
    public class Make : Command
    {

        private const string CommandDescription = "Build canvas app from source";

        public Make(string name) : base(name, CommandDescription)
        {
            AddOption(new Option<int>(new[]
            {
                "--ms-app-path"
            }, "Canvas app path"));
            AddOption(new Option<int>(new[]
            {
                "--packages-path"
            }, "Packages path"));
            AddOption(new Option<int>(new[]
            {
                "--input-app"
            }, "Input Canvas App"));
        }
    }
}
