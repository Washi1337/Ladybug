using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class KillCommand : ICommand
    {
        public string Description
        {
            get { return "Terminates the inferior process."; }
        }

        public string Usage
        {
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            foreach (var process in session.GetProcesses())
                process.Terminate();
        }
    }
}