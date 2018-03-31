using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AsmResolver.X86;
using Ladybug.Core;
using Ladybug.Core.Windows;

namespace Ladybug.Console
{
    internal class Program
    {
        private static ProcessMemoryReader _reader;
        private static X86Disassembler _disassembler;
        private static IDebuggerSession _session;
        private static IDebuggeeThread _currentThread;
        
        private static Logger _logger = new Logger();
        private static InstructionPrinter _printer = new InstructionPrinter();

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
            
            var process = _session.StartProcess(new DebuggerProcessStartInfo
            {
                CommandLine = string.Join(" ", args) 
            });

            _reader = new ProcessMemoryReader(process);
            _disassembler = new X86Disassembler(_reader);

            bool exit = false;
            while (!exit)
            {
                string commandLine = System.Console.ReadLine();
                var commandArgs = commandLine?.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (commandArgs?.Length > 0)
                {
                    switch (commandArgs[0])
                    {
                        case "g":
                        case "go":
                            HandleGoCommand(commandArgs);
                            break;
                        case "bp":
                        case "breakpoint":
                            HandleBreakpointCommand(commandArgs);
                            break;
                        case "bps":
                        case "breakpoints":
                            HandleBreakpointsCommand(commandArgs);
                            break;
                        case "d":
                        case "disassemble":
                            HandleDisassembleCommand(commandArgs);
                            break;
                        case "r":
                        case "registers":
                            HandleRegistersCommand();
                            break;
                        case "break":
                            process.Break();
                            break;
                        case "exit":
                            exit = true;
                            break;
                    }
                }
            }
        }

        private static void HandleBreakpointCommand(string[] commandArgs)
        {
            if (commandArgs.Length > 1)
            {
                if (ulong.TryParse(commandArgs[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out ulong address))
                {
                    _session.GetProcesses().First().SetSoftwareBreakpoint((IntPtr) address);
                    return;
                }
            }
            _logger.WriteLine("Usage: breakpoint <address>");
        }

        private static void HandleBreakpointsCommand(string[] commandArgs)
        {
            foreach (var breakpoint in _session.GetAllBreakpoints())
            {
                _logger.WriteLine("{0:X8} : {1}", breakpoint.Address.ToInt64(),
                    breakpoint.Enabled ? "Enabled" : "Disabled");
            }
        }

        private static void HandleGoCommand(string[] commandArgs)
        {
            if (commandArgs.Length > 1)
            {
                if (commandArgs[1] == "pass")
                {
                    _session.Continue(DebuggerAction.ContinueWithException);
                    return;
                }
            }
            _session.Continue(DebuggerAction.Continue);
        }

        private static void HandleDisassembleCommand(string[] commandArgs)
        {
            switch (commandArgs.Length)
            {
                case 2:
                    _reader.Position = long.Parse(commandArgs[1], NumberStyles.HexNumber);
                    break;
            }
            
            for (int i = 0; i < 5; i++)
                _printer.PrintInstruction(_disassembler.ReadNextInstruction());
        }

        private static void HandleRegistersCommand()
        {
            DumpRegisters(_currentThread.GetThreadContext());
        }

        private static void DumpRegisters(IThreadContext context)
        {
            foreach (var register in context.GetTopLevelRegisters())
            {
                ulong value = Convert.ToUInt64(register.Value);
                _logger.WriteLine("{0}: 0x{1} ({2})", register.Name, value.ToString("X" + register.Size / 4), value);
            }
        }

        private static void SessionOnOutputStringSent(object sender, DebuggeeOutputStringEventArgs args)
        {
            _logger.WriteLine(LoggerMessageType.OutputString, "Debuggee sent debug message: " + args.Message);
        }

        private static void SessionOnPaused(object sender, DebuggeeThreadEventArgs args)
        {
            _logger.WriteLine("Debuggee paused.");
            _currentThread = args.Thread;
            var threadContext = _currentThread.GetThreadContext();
            
            _logger.WriteLine("Thread ID: " + args.Thread.Id);
            DumpRegisters(threadContext);
            _reader.Position = (uint) threadContext.GetRegisterByName("eip").Value;
            _printer.PrintInstruction(_disassembler.ReadNextInstruction());
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
            _logger.WriteLine(LoggerMessageType.Error, "{0} exception occurred in thread {1} with error code {2:X}. {3}. Error is {4}.",
                args.Exception.IsFirstChance ? "First chance" : "Last chance",
                args.Thread.Id, 
                args.Exception.ErrorCode,
                args.Exception.Message,
                args.Exception.Continuable ? "continuable" : "uncontinuable");
        }

        private static void SessionOnBreakpointHit(object sender, BreakpointEventArgs breakpointEventArgs)
        {
            _logger.WriteLine(LoggerMessageType.Breakpoint, "Breakpoint at address {0:X8} hit.",
                breakpointEventArgs.Breakpoint.Address.ToInt64());
        }
    }
}