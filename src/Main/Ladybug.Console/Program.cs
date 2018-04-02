using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using AsmResolver.X86;
using Ladybug.Console.Commands;
using Ladybug.Core;
using Ladybug.Core.Windows;

namespace Ladybug.Console
{
    internal class Program
    {
        private static IDebuggerSession _session;
        private static IDebuggeeThread _currentThread;
        
        private static Logger _logger = new Logger();
        private static InstructionPrinter _printer = new InstructionPrinter();

        private static bool _justStepped = false;
        private static CommandExecutor _executor;

        public static void Main(string[] args)
        {
            if (args.Length == 0)
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
            
            var process = _session.StartProcess(new DebuggerProcessStartInfo
            {
                CommandLine = string.Join(" ", args) 
            });


            _executor = new CommandExecutor(_logger);
            _executor.RegisterCommand(new GoCommand(), "g", "go");
            _executor.RegisterCommand(new StepCommand(), "s", "step");
            _executor.RegisterCommand(new DumpMemoryCommand(), "dm", "dump");
            _executor.RegisterCommand(new ModulesCommand(), "m", "modules");
            _executor.RegisterCommand(new BreakpointCommand(), "bp", "breakpoint");
            _executor.RegisterCommand(new BreakpointsCommand(), "bps", "breakpoints");
            _executor.RegisterCommand(new DisassembleCommand(), "d", "disassemble");
            _executor.RegisterCommand(new RegisterCommand(), "r", "registers");
            _executor.RegisterCommand(new BreakCommand(), "break");
            
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

        private static void SessionOnOutputStringSent(object sender, DebuggeeOutputStringEventArgs args)
        {
            _logger.WriteLine(LoggerMessageType.OutputString, "Debuggee sent debug message: " + args.Message);
        }

        private static void SessionOnPaused(object sender, DebuggeeThreadEventArgs args)
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

        private static void SessionOnStepped(object sender, DebuggeeThreadEventArgs args)
        {
            _justStepped = true;
        }

        private static void SessionOnProcessStarted(object sender, DebuggeeProcessEventArgs args)
        {
            var thread = args.Process.Threads.First();
            _logger.WriteLine("Process {0} created with thread {1} at address {2:X}.", args.Process.Id, thread.Id, thread.StartAddress.ToInt64());
            args.NextAction = DebuggerAction.Stop;
        }

        private static void SessionOnProcessTerminated(object sender, DebuggeeProcessEventArgs args)
        {
            _logger.WriteLine("Process terminated. ID: " + args.Process.Id);
        }

        private static void SessionOnThreadStarted(object sender, DebuggeeThreadEventArgs args)
        {
            _logger.WriteLine("Thread created. ID: " + args.Thread.Id);
        }

        private static void SessionOnThreadTerminated(object sender, DebuggeeThreadEventArgs args)
        {
            _logger.WriteLine("Thread terminated. ID: " + args.Thread.Id);
        }

        private static void SessionOnLibraryLoaded(object sender, DebuggeeLibraryEventArgs args)
        {
            _logger.WriteLine("Loaded library " + (args.Library.Name ?? "<no name>") + " at "
                                     + args.Library.BaseOfLibrary.ToInt64().ToString("X8"));
        }

        private static void SessionOnLibraryUnloaded(object sender, DebuggeeLibraryEventArgs args)
        {
            _logger.WriteLine("Unloaded library " + (args.Library.Name ?? "<no name>") + " at "+ args.Library.BaseOfLibrary.ToInt64().ToString("X8"));
        }

        private static void SessionOnExceptionOccurred(object sender, DebuggeeExceptionEventArgs args)
        {
            _logger.WriteLine(LoggerMessageType.Error, "{0} exception occurred in thread {1} with error code {2:X}. {3}.",
                args.Exception.IsFirstChance ? "First chance" : "Last chance",
                args.Thread.Id, 
                args.Exception.ErrorCode,
                args.Exception.Message,
                args.Exception.Continuable ? string.Empty : "Exception is fatal.");
        }

        private static void SessionOnBreakpointHit(object sender, BreakpointEventArgs breakpointEventArgs)
        {
            _logger.WriteLine(LoggerMessageType.Breakpoint, "Breakpoint at address {0:X8} hit.",
                breakpointEventArgs.Breakpoint.Address.ToInt64());
        }
    }
}