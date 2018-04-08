using System;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class StepInCommand : ICommand
    {
        public string Description
        {
            get { return "Executes the instruction at the instruction pointer."; }
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
                
                session.Step(StepType.StepIn, DebuggerAction.ContinueWithException);
            }
            
            session.Step(StepType.StepIn, DebuggerAction.Continue);
        }
    }
}