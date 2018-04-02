using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
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

        private static bool _justStepped = false;

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
                        case "s":
                        case "step":
                            HandleStepCommand(commandArgs);
                            break;
                        case "dm":
                        case "dump":
                            HandleDumpMemoryCommand(commandArgs);
                            break;
                        case "m":
                        case "modules":
                            HandleListModulesCommand(commandArgs);
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
                            HandleRegistersCommand(commandArgs);
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

        private static void HandleStepCommand(string[] commandArgs)
        {
            _session.Step(DebuggerAction.Continue);
        }

        private static void HandleDumpMemoryCommand(string[] commandArgs)
        {
            if (commandArgs.Length > 1)
            {
                if (ulong.TryParse(commandArgs[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out ulong address))
                {
                    var size = X86OperandSize.Byte;
                    if (commandArgs.Length > 2)
                    {
                        switch (commandArgs[2].ToLowerInvariant())
                        {
                            case "b":
                                size = X86OperandSize.Byte;
                                break;
                            case "w":
                                size = X86OperandSize.Word;
                                break;
                            case "dw":
                                size = X86OperandSize.Dword;
                                break;
                        } 
                    }
                    DumpMemory(address, 5, size);
                    return;
                }
            }
            _logger.WriteLine("Usage: dump <address> [b|w|dw]");
        }

        private static void HandleListModulesCommand(string[] commandArgs)
        {
            foreach (var lib in _session.GetProcesses().First().Libraries)
            {
                _logger.WriteLine("{0:X8}: {1}", lib.BaseOfLibrary.ToInt64(), (lib.Name ?? "<no name>"));
            }
        }

        private static void HandleBreakpointCommand(string[] commandArgs)
        {
            if (commandArgs.Length > 2)
            {
                if (ulong.TryParse(commandArgs[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out ulong address))
                {
                    var debuggeeProcess = _session.GetProcesses().First();
                    switch (commandArgs[1].ToLowerInvariant())
                    {
                        case "set":
                            debuggeeProcess.SetSoftwareBreakpoint((IntPtr) address);
                            break;
                        case "remove":
                            debuggeeProcess.RemoveSoftwareBreakpoint(
                                debuggeeProcess.GetBreakpintByAddress((IntPtr) address));
                            break;
                        case "enable":
                            debuggeeProcess.GetBreakpintByAddress((IntPtr) address).Enabled = true;
                            break;
                        case "disable":
                            debuggeeProcess.GetBreakpintByAddress((IntPtr) address).Enabled = false;
                            break;
                    }
                    return;
                }
            }
            _logger.WriteLine("Usage: breakpoint <set|remove|enable|disable> <address>");
        }

        private static void HandleBreakpointsCommand(string[] commandArgs)
        {
            foreach (var breakpoint in _session.GetAllBreakpoints())
            {
                _logger.WriteLine("{0:X8} : {1}", breakpoint.Address.ToInt64(),
                    breakpoint.Enabled ? "Enabled" : "Disabled");
            }
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

        private static void HandleRegistersCommand(string[] commandArgs)
        {
            var threadContext = _currentThread.GetThreadContext();
            
            if (commandArgs.Length > 2)
            {
                string regName = commandArgs[1];
                string regValue = commandArgs[2];

                var register = threadContext.GetRegisterByName(regName);
                switch (register.Size)
                {
                    case 1:
                        register.Value = regValue == "1";
                        break;
                    case 8:
                        register.Value = byte.Parse(regValue, NumberStyles.HexNumber);
                        break;
                    case 16:
                        register.Value = ushort.Parse(regValue, NumberStyles.HexNumber);
                        break;
                    case 32:
                        register.Value = uint.Parse(regValue, NumberStyles.HexNumber);
                        break;
                    case 64:
                        register.Value = ulong.Parse(regValue, NumberStyles.HexNumber);
                        break;
                    default:
                        break;
                }
                
                threadContext.Flush();
            }

            DumpRegisters(threadContext);
        }

        private static void DumpRegisters(IThreadContext context)
        {
            foreach (var register in context.GetTopLevelRegisters())
            {
                ulong value = Convert.ToUInt64(register.Value);
                _logger.WriteLine("{0}: 0x{1} ({2})", register.Name, value.ToString("X" + register.Size / 4), value);
            }
        }

        private static void DumpMemory(ulong address, int rows, X86OperandSize size)
        {
            const int rowSize = 0x10;
            
            var buffer = new byte[rows * rowSize];
            _session.GetProcesses().First().ReadMemory((IntPtr) address, buffer, 0, buffer.Length);

            int stepSize = 0;
            switch (size)
            {
                case X86OperandSize.Byte:
                    stepSize = 1;
                    break;
                case X86OperandSize.Word:
                    stepSize = 2;
                    break;
                case X86OperandSize.Dword:
                    stepSize = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size));
            }

            for (int row = 0; row < rows; row++)
            {
                _logger.Write("{0:X8}:  ", address + (ulong) (row * rowSize));

                var builder = new StringBuilder();
                for (int col = 0; col < rowSize; col += stepSize)
                {
                    if (col % 4 == 0)
                        _logger.Write(" ");
                    
                    ulong currentValue = 0;
                    switch (size)
                    {
                        case X86OperandSize.Byte:
                            currentValue = buffer[row * rowSize + col];
                            break;
                        case X86OperandSize.Word:
                            currentValue = BitConverter.ToUInt16(buffer, row * rowSize + col);
                            break;
                        case X86OperandSize.Dword:
                            currentValue = BitConverter.ToUInt32(buffer, row * rowSize + col);
                            break;
                    }
                    
                    _logger.Write("{0} ", currentValue.ToString("X" + (stepSize * 2)));

                    if (stepSize == 1) 
                    {
                        char currentChar = (char) currentValue;
                        builder.Append(char.IsControl(currentChar) ? '.' : currentChar);
                    }
                }

                _logger.WriteLine("  " + builder.ToString());
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
                DumpRegisters(threadContext);
            }

            _reader.Position = (uint) threadContext.GetRegisterByName("eip").Value;

            _printer.PrintInstruction(_disassembler.ReadNextInstruction());

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