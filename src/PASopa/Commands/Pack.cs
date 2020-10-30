using System.CommandLine;

namespace PASopa.Commands
{
    public class Pack : Command
    {
        private const string CommandDescription = "Pack canvas app for deployment";

        public Pack(string name) : base(name, CommandDescription)
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
