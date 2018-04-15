using System;
using System.Globalization;
using System.Linq;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class MemoryBreakpointCommand : ICommand
    {
        public string Description
        {
            get { return "Sets or changes the behaviour of memory breakpoints in the process."; }
        }

        public string Usage
        {
            get { return "<set|remove|enable|disable> <address>"; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            if (arguments.Length < 2)
                throw new ArgumentException("Not enough arguments.");
            
            ulong address = ulong.Parse(arguments[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            var debuggeeProcess = session.GetProcesses().First();
            switch (arguments[0].ToLowerInvariant())
            {
                case "set":
                    var breakpoint = debuggeeProcess.SetMemoryBreakpoint((IntPtr) address);
                    breakpoint.BreakOnRead = breakpoint.BreakOnWrite = true;
                    break;
                case "remove":
//                    debuggeeProcess.RemoveMemoryBreakpoint(
//                        debuggeeProcess.GetMemoryBreakpointByAddress((IntPtr) address));
                    break;
                case "enable":
//                    debuggeeProcess.GetSoftwareBreakpointByAddress((IntPtr) address).Enabled = true;
                    break;
                case "disable":
//                    debuggeeProcess.GetSoftwareBreakpointByAddress((IntPtr) address).Enabled = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid switch " + arguments[1]);
            }

        }
    }
}