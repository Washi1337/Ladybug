using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class BreakCommand : ICommand
    {
        public string Description
        {
            get { return "Pauses all running debuggees."; }
        }

        public string Usage
        {
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            foreach (var process in session.GetProcesses())
                process.Break();
        }
    }
}