using System.Linq;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class BreakpointsCommand : ICommand
    {
        public string Description
        {
            get { return "Lists all currently set breakpoints."; }
        }

        public string Usage
        {
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            output.WriteLine("Software Breakpoints:");
            foreach (var breakpoint in session.GetProcesses().SelectMany(x => x.GetSoftwareBreakpoints()))
            {
                output.WriteLine("- {0:X8} : {1}", breakpoint.Address.ToInt64(),
                    breakpoint.Enabled ? "Enabled" : "Disabled");
            }
            
            output.WriteLine("Memory Breakpoints:");
            foreach (var breakpoint in session.GetProcesses().SelectMany(x => x.GetMemoryBreakpoints()))
            {
                output.WriteLine("- {0:X8} : {1}", breakpoint.Address.ToInt64(),
                    breakpoint.Enabled ? "Enabled" : "Disabled");
            }
        }
    }
}