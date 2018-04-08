using System;
using System.Globalization;
using System.Linq;
using System.Text;
using AsmResolver.X86;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class DumpMemoryCommand : ICommand
    {
        public string Description
        {
            get { return "Dumps memory from the inferior process."; }
        }

        public string Usage
        {
            get { return "<address> [b|w|dw]"; }
        }
        
        private void DumpMemory(IDebuggeeProcess process, ulong address, int rows, X86OperandSize size, Logger logger)
        {
            const int rowSize = 0x10;
            
            var buffer = new byte[rows * rowSize];
            process.ReadMemory((IntPtr) address, buffer, 0, buffer.Length);

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
                logger.Write("{0:X8}:  ", address + (ulong) (row * rowSize));

                var builder = new StringBuilder();
                for (int col = 0; col < rowSize; col += stepSize)
                {
                    if (col % 4 == 0)
                        logger.Write(" ");
                    
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
                    
                    logger.Write("{0} ", currentValue.ToString("X" + (stepSize * 2)));

                    if (stepSize == 1) 
                    {
                        char currentChar = (char) currentValue;
                        builder.Append(char.IsControl(currentChar) ? '.' : currentChar);
                    }
                }

                logger.WriteLine("  " + builder.ToString());
            }
        }


        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            if (arguments.Length == 0)
                throw new ArgumentException("Expected address.");

            ulong address = ulong.Parse(arguments[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            var size = X86OperandSize.Byte;
            if (arguments.Length > 1)
            {
                switch (arguments[1].ToLowerInvariant())
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
                    default:
                        throw new ArgumentOutOfRangeException("Invalid switch "  + arguments[1]);
                }
            }

            var process = session.GetProcesses().First();
            DumpMemory(process, address, 5, size, output);
        }
    }
}