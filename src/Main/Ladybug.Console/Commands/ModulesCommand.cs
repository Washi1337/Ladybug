using System.Linq;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class ModulesCommand : ICommand
    {
        public string Description
        {
            get { return "Lists the libraries currently loaded in the inferior process."; }
        }

        public string Usage
        {
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            foreach (var lib in session.GetProcesses().First().Libraries)
            {
                output.WriteLine("{0:X8}: {1}", lib.BaseOfLibrary.ToInt64(), (lib.Name ?? "<no name>"));
            }
        }
    }
}