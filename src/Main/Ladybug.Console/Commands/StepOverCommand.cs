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
            get { return string.Empty; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            session.Step(StepType.StepOver, DebuggerAction.Continue);
        }
    }
}