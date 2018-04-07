using System;
using System.Globalization;
using System.Linq;
using AsmResolver;
using AsmResolver.X86;
using Ladybug.Core;
using Ladybug.Core.Windows;

namespace Ladybug.Console.Commands
{
    public class DisassembleCommand : ICommand
    {
        private readonly InstructionPrinter _printer = new InstructionPrinter();
        private IDebuggeeProcess _process;
        private ProcessMemoryReader _reader;
        private X86Disassembler _disassembler;

        public string Description
        {
            get { return "Disassembles the inferior process at a given address, or continues disassembling."; }
        }

        public string Usage
        {
            get { return "[address [count]]"; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            var process = session.GetProcesses().First();
            if (process != _process)
            {
                _process = process;
                _reader = new ProcessMemoryReader(process) {NegateBreakpoints = true};
                _disassembler = new X86Disassembler(_reader);
            }

            if (arguments.Length > 0)
                _reader.Position = long.Parse(arguments[0], NumberStyles.HexNumber);

            int count = 5;
            if (arguments.Length > 1)
                count = int.Parse(arguments[1]);
            
            for (int i = 0; i < count; i++)
            {
                var instruction = _disassembler.ReadNextInstruction();
                _printer.PrintInstruction(instruction, process.GetBreakpointByAddress((IntPtr) instruction.Offset));
            }
        }
    }
}