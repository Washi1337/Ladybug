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
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            session.Step(StepType.StepIn, DebuggerAction.Continue);
        }
    }
}