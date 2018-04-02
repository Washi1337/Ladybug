using System;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class GoCommand : ICommand
    {
        public string Description
        {
            get { return "Signals the inferior process to continue execution."; }
        }

        public string Usage
        {
            get { return "[pass]"; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            if (arguments.Length > 0)
            {
                if (arguments[0] != "pass")
                    throw new ArgumentException("Invalid switch " + arguments[0]);
                
                session.Continue(DebuggerAction.ContinueWithException);
            }
            
            session.Continue(DebuggerAction.Continue);
        }
    }
}