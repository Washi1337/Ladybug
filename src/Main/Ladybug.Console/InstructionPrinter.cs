using System;
using System.Collections.Generic;
using AsmResolver.X86;

namespace Ladybug.Console
{
    public class InstructionPrinter
    {
        private readonly FasmX86Formatter _formatter;

        public InstructionPrinter()
        {
            _formatter = new FasmX86Formatter();
            
            MnemonicColors = new Dictionary<X86Mnemonic, ConsoleColor>
            {
                [X86Mnemonic.Call] = ConsoleColor.Cyan,
                [X86Mnemonic.Call_Far] = ConsoleColor.Cyan,
                [X86Mnemonic.Retf] = ConsoleColor.Cyan,
                [X86Mnemonic.Retn] = ConsoleColor.Cyan,
                
                [X86Mnemonic.Ja] = ConsoleColor.Yellow,
                [X86Mnemonic.Jb] = ConsoleColor.Yellow,
                [X86Mnemonic.Jbe] = ConsoleColor.Yellow,
                [X86Mnemonic.Je] = ConsoleColor.Yellow,
                [X86Mnemonic.Jecxz] = ConsoleColor.Yellow,
                [X86Mnemonic.Jg] = ConsoleColor.Yellow,
                [X86Mnemonic.Jge] = ConsoleColor.Yellow,
                [X86Mnemonic.Jl] = ConsoleColor.Yellow,
                [X86Mnemonic.Jle] = ConsoleColor.Yellow,
                [X86Mnemonic.Jmp] = ConsoleColor.Yellow,
                [X86Mnemonic.Jmp_Far] = ConsoleColor.Yellow,
                [X86Mnemonic.Jnb] = ConsoleColor.Yellow,
                [X86Mnemonic.Jne] = ConsoleColor.Yellow,
                [X86Mnemonic.Jno] = ConsoleColor.Yellow,
                [X86Mnemonic.Jns] = ConsoleColor.Yellow,
                [X86Mnemonic.Jo] = ConsoleColor.Yellow,
                [X86Mnemonic.Jpe] = ConsoleColor.Yellow,
                [X86Mnemonic.Jpo] = ConsoleColor.Yellow,
                [X86Mnemonic.Js] = ConsoleColor.Yellow,
            };

        }

        public IDictionary<X86Mnemonic, ConsoleColor> MnemonicColors
        {
            get;
        }
        
        public void PrintInstruction(X86Instruction instruction)
        {
            var original = System.Console.ForegroundColor;
            
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.Write(instruction.Offset.ToString("X8") + ": ");

            ConsoleColor color;
            if (!MnemonicColors.TryGetValue(instruction.Mnemonic, out color))
                color = ConsoleColor.DarkGray;

            System.Console.ForegroundColor = color;
            System.Console.Write(_formatter.FormatMnemonic(instruction.Mnemonic));

            if (instruction.Operand1 != null)
            {
                System.Console.Write(' ');
                System.Console.Write(_formatter.FormatOperand(instruction.Operand1));
                
                if (instruction.Operand2 != null)
                {
                    System.Console.Write(", ");
                    System.Console.Write(_formatter.FormatOperand(instruction.Operand2));
                }
            }

            System.Console.WriteLine();
            System.Console.ForegroundColor = original;
        }
    }
}