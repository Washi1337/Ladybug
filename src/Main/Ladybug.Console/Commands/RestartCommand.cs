using System.Collections.Generic;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class RestartCommand : ICommand
    {
        public RestartCommand(IEnumerable<string> commandLineArgs)
        {
            CommandLineArgs = new List<string>(commandLineArgs);
        }

        public IList<string> CommandLineArgs
        {
            get;
        }

        public string Description
        {
            get { return "Restarts the inferior process."; }
        }

        public string Usage
        {
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            foreach (var process in session.GetProcesses())
                process.Terminate();
            
            session.StartProcess(new DebuggerProcessStartInfo
            {
                CommandLine = string.Join(" ", CommandLineArgs) 
            });
        }
    }
}