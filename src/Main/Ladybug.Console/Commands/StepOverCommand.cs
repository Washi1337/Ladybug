using System;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class StepOverCommand : ICommand
    {
        public string Description
        {
            get { return "Executes until the next instruction line."; }
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
                
                session.Step(StepType.StepOver, DebuggerAction.ContinueWithException);
            }
            
            session.Step(StepType.StepOver, DebuggerAction.Continue);
        }
    }
}