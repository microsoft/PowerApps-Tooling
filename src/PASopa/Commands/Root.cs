using System.CommandLine;

namespace PASopa.Commands
{
    public class Root: Command
    {
        private const string CommandDescription = "Canvas Apps Solution Packager";

        public Root(string name) : base(name, CommandDescription)
        {
            AddCommand(new Test(nameof(Test).ToLower()));
            AddCommand(new UnPack(nameof(UnPack).ToLower()));
            AddCommand(new Pack(nameof(Pack).ToLower()));
            AddCommand(new Make(nameof(Make).ToLower()));
        }
    }
}
