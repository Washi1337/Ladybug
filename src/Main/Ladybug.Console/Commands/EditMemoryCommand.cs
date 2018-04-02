using System;
using System.Globalization;
using System.Linq;
using AsmResolver.X86;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class EditMemoryCommand : ICommand
    {
        public string Description
        {
            get { return "Updates raw memory of the inferior process."; }
        }

        public string Usage
        {
            get { return "<address> <b|w|dw> v1 [v2 v3 ...]"; }
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            var process = session.GetProcesses().First();
            
            if (arguments.Length < 1)
                throw new ArgumentException("Expected address.");
            if (arguments.Length < 2)
                throw new ArgumentException("Expected value size.");
            if (arguments.Length < 3)
                throw new ArgumentException("Expected at least one value to write.");

            ulong address = ulong.Parse(arguments[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            int size = 1;
            switch (arguments[1].ToLowerInvariant())
            {
                case "b":
                    size = 1;
                    break;
                case "w":
                    size = 2;
                    break;
                case "dw":
                    size = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid switch "  + arguments[1]);
            }

            var buffer = new byte[(arguments.Length - 2) * size];
            
            for (int i = 0; i < arguments.Length - 2; i++)
            {
                ulong currentValue = ulong.Parse(arguments[i + 2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var bytes = BitConverter.GetBytes(currentValue);
                Buffer.BlockCopy(bytes, 0, buffer, i * size, size);
            }

            process.WriteMemory((IntPtr) address, buffer, 0, buffer.Length);

        }
    }
}