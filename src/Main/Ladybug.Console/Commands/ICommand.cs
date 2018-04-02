using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public interface ICommand
    {
        string Description
        {
            get;
        }

        string Usage
        {
            get;
        }

        void Execute(IDebuggerSession session, string[] arguments, Logger output);
    }
}