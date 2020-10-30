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
                "-map",
                "/map",
                "--ms-app-path"
            }, "Canvas app path"));
            AddOption(new Option<int>(new[]
            {
                "-dir",
                "/dir",
                "--input-dir-path"
            }, "Input directory path"));
        }
    }
}
