using System;
using System.Collections.Generic;
using System.Linq;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class CommandExecutor
    {
        private sealed class HelpCommand : ICommand
        {
            private CommandExecutor _parent;

            public HelpCommand(CommandExecutor parent)
            {
                _parent = parent;
            }
            
            public string Description
            {
                get { return "Gives an overview of all available commands."; }
            }

            public string Usage
            {
                get { return "[command]"; }
            }

            public void Execute(IDebuggerSession session, string[] arguments, Logger output)
            {
                switch (arguments.Length)
                {
                    case 0:
                    {
                        int maxLength = _parent._prefixesByCommand.Max(x => string.Join(", ", x.Value).Length);
                        foreach (var command in _parent._prefixesByCommand.OrderBy(x => x.Value[0]))
                        {
                            output.WriteLine(string.Join(", ", command.Value).PadRight(maxLength + 5)
                                             + command.Key.Description);
                        }

                        break;
                    }

                    case 1:
                    {
                        var command = _parent._commands[arguments[0]];
                        output.WriteLine("Description:         " +  command.Description);
                        output.WriteLine("Usage:               " + arguments[0] + " " + command.Usage);
                        output.WriteLine("Equivalent commands: " + string.Join(", ",
                            _parent._prefixesByCommand[command].Where(x => x != arguments[0])));
                        break;
                    }
                }
            }
        }
        
        private readonly Logger _logger;
        private readonly IDictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();
        private readonly IDictionary<ICommand, string[]> _prefixesByCommand = new Dictionary<ICommand, string[]>();

        public CommandExecutor(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RegisterCommand(new HelpCommand(this), "help");
        }

        public TCommand GetCommand<TCommand>()
        {
            return _commands.Values.OfType<TCommand>().First();
        }
        
        public void RegisterCommand(ICommand command, params string[] prefixes)
        {
            _prefixesByCommand.Add(command, prefixes);
            foreach (var prefix in prefixes)
                _commands.Add(prefix, command);
        }

        public void ExecuteCommandLine(IDebuggerSession session, string commandLine)
        {
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                var args = commandLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (!_commands.TryGetValue(args[0], out var command))
                    throw new ArgumentException("Command " + args[0] + " is not a registered command.");

                command.Execute(session, args.Skip(1).ToArray(), _logger);
            }
        }
    }
}