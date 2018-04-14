using System.Linq;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class ThreadsCommand : ICommand
    {
        public string Description
        {
            get { return "Gets a list of all running threads in the inferior process."; }
        }

        public string Usage
        {
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            foreach (var thread in session.GetProcesses().First().Threads)
                output.WriteLine("{0}: {1:X8}", thread.Id, thread.StartAddress.ToInt64());
        }
    }
}