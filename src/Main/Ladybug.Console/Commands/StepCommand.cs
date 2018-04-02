using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class StepCommand : ICommand
    {
        public string Description
        {
            get { return "Executes the instruction at the instruction pointer."; }
        }

        public string Usage
        {
            get;
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            session.Step(DebuggerAction.Continue);
        }
    }
}