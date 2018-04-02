using System;
using System.Collections.Generic;
using System.Linq;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class CommandExecutor
    {
        private readonly Logger _logger;
        private readonly IDictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        public CommandExecutor(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TCommand GetCommand<TCommand>()
        {
            return _commands.Values.OfType<TCommand>().First();
        }
        
        public void RegisterCommand(ICommand command, params string[] prefixes)
        {
            foreach (var prefix in prefixes)
                _commands.Add(prefix, command);
        }

        public void ExecuteCommandLine(IDebuggerSession session, string commandLine)
        {
            var args = commandLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (!_commands.TryGetValue(args[0], out var command))
                throw new ArgumentException("Command " + args[0] + " is not a registered command.");
            
            command.Execute(session, args.Skip(1).ToArray(), _logger);
        }
    }
}