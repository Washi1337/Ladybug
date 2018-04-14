using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ladybug.Console.Commands;
using Ladybug.Core;
using Ladybug.Core.Windows;

namespace Ladybug.Console
{
    public class ConsoleDebugger
    {
        private IDebuggerSession _session;
        private IDebuggeeThread _currentThread;
        private readonly Logger _logger = new Logger();
        private InstructionPrinter _printer = new InstructionPrinter();
        private bool _justStepped = false;
        private CommandExecutor _executor;

        public ConsoleDebugger(IEnumerable<string> commandLineArgs)
        {
            CommandLineArgs = new List<string>(commandLineArgs);
        }
        
        public IList<string> CommandLineArgs
        {
            get;
        }

        public void Run()
        {
            PrintAbout();
            
            if (CommandLineArgs.Count == 0)
            {
                _logger.WriteLine("Usage: Ladybug.Console.exe <file> [arguments]");
                return;
            }
            
            _session = new DebuggerSession();
            _session.ProcessStarted += SessionOnProcessStarted;
            _session.ProcessTerminated += SessionOnProcessTerminated;
            _session.ThreadStarted += SessionOnThreadStarted;
            _session.ThreadTerminated += SessionOnThreadTerminated;
            _session.OutputStringSent += SessionOnOutputStringSent;
            _session.LibraryLoaded += SessionOnLibraryLoaded;
            _session.LibraryUnloaded += SessionOnLibraryUnloaded;
            _session.ExceptionOccurred += SessionOnExceptionOccurred;
            _session.BreakpointHit += SessionOnBreakpointHit;
            _session.Paused += SessionOnPaused;
            _session.Stepped  += SessionOnStepped;
            
            _session.StartProcess(new DebuggerProcessStartInfo
            {
                CommandLine = string.Join(" ", CommandLineArgs) 
            });

            _executor = new CommandExecutor(_logger);
            _executor.RegisterCommand(new KillCommand(), "kill");
            _executor.RegisterCommand(new RestartCommand(CommandLineArgs), "restart");
            _executor.RegisterCommand(new GoCommand(), "go", "g");
            _executor.RegisterCommand(new StepInCommand(), "stepin", "si");
            _executor.RegisterCommand(new StepOverCommand(), "stepover", "so");
            _executor.RegisterCommand(new DumpMemoryCommand(), "dump", "dm");
            _executor.RegisterCommand(new EditMemoryCommand(), "write", "wm");
            _executor.RegisterCommand(new ModulesCommand(), "modules", "m");
            _executor.RegisterCommand(new BreakpointCommand(), "breakpoint", "bp");
            _executor.RegisterCommand(new BreakpointsCommand(), "breakpoints", "bps");
            _executor.RegisterCommand(new DisassembleCommand(), "disassemble", "d");
            _executor.RegisterCommand(new RegisterCommand(), "registers", "r");
            _executor.RegisterCommand(new BreakCommand(), "break");
            _executor.RegisterCommand(new ThreadsCommand(), "threads");
            
            bool exit = false;
            while (!exit)
            {
                string commandLine = System.Console.ReadLine();
                if (commandLine == "exit")
                {
                    exit = true;
                }
                else
                {
                    try
                    {
                        _executor.ExecuteCommandLine(_session, commandLine);
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLine(LoggerMessageType.Error, ex.ToString());
                    }
                }
            }
        }

        private void PrintAbout()
        {
            var consoleAppInfo = FileVersionInfo.GetVersionInfo(typeof(ConsoleDebugger).Assembly.Location);
            var coreAppInfo = FileVersionInfo.GetVersionInfo(typeof(IDebuggerSession).Assembly.Location);
            var backendInfo = FileVersionInfo.GetVersionInfo(typeof(DebuggerSession).Assembly.Location);
            
            var assemblyName = typeof(ConsoleDebugger).Assembly.GetName();
            _logger.WriteLine(LoggerMessageType.Default,
                $"{assemblyName.Name}, v{consoleAppInfo.FileVersion}, Core v{coreAppInfo.FileVersion}, Windows backend v{backendInfo.FileVersion}\n" +
                $"Copyright: {consoleAppInfo.LegalCopyright}\n" + 
                $"Repository and issue tracker: https://github.com/Washi1337/Ladybug");
        }
        
        private void SessionOnOutputStringSent(object sender, DebuggeeOutputStringEventArgs args)
        {
            _logger.WriteLine(LoggerMessageType.OutputString, "Debuggee sent debug message: " + args.Message);
        }

        private void SessionOnPaused(object sender, DebuggeeThreadEventArgs args)
        {
            _currentThread = args.Thread;
            var threadContext = _currentThread.GetThreadContext();

            if (!_justStepped)
            {
                _logger.WriteLine("Debuggee paused.");
                _logger.WriteLine("Thread ID: " + args.Thread.Id);
                
                var registerCommand = _executor.GetCommand<RegisterCommand>();
                registerCommand.SetCurrentThread(_currentThread);
                registerCommand.Execute(_session, new string[0], _logger);
            }

            uint eip = (uint) threadContext.GetRegisterByName("eip").Value;
            
            _executor.GetCommand<DisassembleCommand>().Execute(_session, new[] {eip.ToString("X8"), "1" }, _logger);

            _justStepped = false;
        }

        private void SessionOnStepped(object sender, DebuggeeThreadEventArgs args)
        {
            _justStepped = true;
        }

        private void SessionOnProcessStarted(object sender, DebuggeeProcessEventArgs args)
        {
            var thread = args.Process.Threads.First();
            _logger.WriteLine("Process {0} created with thread {1} at address {2:X}.", args.Process.Id, thread.Id, thread.StartAddress.ToInt64());
//            args.NextAction = DebuggerAction.Stop;
        }

        private void SessionOnProcessTerminated(object sender, DebuggeeProcessEventArgs args)
        {
            _logger.WriteLine("Process terminated. ID: " + args.Process.Id);
        }

        private void SessionOnThreadStarted(object sender, DebuggeeThreadEventArgs args)
        {
            _logger.WriteLine("Thread created. ID: " + args.Thread.Id);
        }

        private void SessionOnThreadTerminated(object sender, DebuggeeThreadEventArgs args)
        {
            _logger.WriteLine("Thread terminated. ID: " + args.Thread.Id);
        }

        private void SessionOnLibraryLoaded(object sender, DebuggeeLibraryEventArgs args)
        {
            _logger.WriteLine("Loaded library " + (args.Library.Name ?? "<no name>") + " at "
                                     + args.Library.BaseOfLibrary.ToInt64().ToString("X8"));
        }

        private void SessionOnLibraryUnloaded(object sender, DebuggeeLibraryEventArgs args)
        {
            _logger.WriteLine("Unloaded library " + (args.Library.Name ?? "<no name>") + " at "+ args.Library.BaseOfLibrary.ToInt64().ToString("X8"));
        }

        private void SessionOnExceptionOccurred(object sender, DebuggeeExceptionEventArgs args)
        {
            _logger.WriteLine(LoggerMessageType.Error, "{0} exception occurred in thread {1} with error code {2:X}. {3}.",
                args.Exception.IsFirstChance ? "First chance" : "Last chance",
                args.Thread.Id, 
                args.Exception.ErrorCode,
                args.Exception.Message,
                args.Exception.Continuable ? string.Empty : "Exception is fatal.");
        }

        private void SessionOnBreakpointHit(object sender, BreakpointEventArgs breakpointEventArgs)
        {
            _logger.WriteLine(LoggerMessageType.Breakpoint, "Breakpoint at address {0:X8} hit.",
                breakpointEventArgs.Breakpoint.Address.ToInt64());
        }
    }
    
}