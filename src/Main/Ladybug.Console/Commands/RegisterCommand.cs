using System;
using System.Globalization;
using Ladybug.Core;

namespace Ladybug.Console.Commands
{
    public class RegisterCommand : ICommand
    {
        private IDebuggeeThread _currentThread;

        public string Description
        {
            get { return "Dumps or updates registers."; }
        }

        public string Usage
        {
            get { return "[reg] [newvalue]"; }
        }

        private static void DumpRegister(IRegister register, Logger output)
        {
            ulong value = Convert.ToUInt64(register.Value);
            output.WriteLine("{0}: 0x{1} ({2})", register.Name, value.ToString("X" + register.Size / 4), value);
        }

        private static void DumpRegisters(IThreadContext context, Logger output)
        {
            foreach (var register in context.GetTopLevelRegisters())
            {
                DumpRegister(register, output);
            }
        }

        public void SetCurrentThread(IDebuggeeThread currentThread)
        {
            _currentThread = currentThread;
        }

        public void Execute(IDebuggerSession session, string[] arguments, Logger output)
        {
            var threadContext = _currentThread.GetThreadContext();
            
            if (arguments.Length == 0)
                DumpRegisters(threadContext, output);
            else if (arguments.Length > 0)
            {
                var register = threadContext.GetRegisterByName(arguments[0]);

                if (arguments.Length < 2)
                {
                    DumpRegister(register, output);
                }
                else
                {
                    string newValue = arguments[1];

                    switch (register.Size)
                    {
                        case 1:
                            register.Value = newValue == "1";
                            break;
                        case 8:
                            register.Value = byte.Parse(newValue, NumberStyles.HexNumber);
                            break;
                        case 16:
                            register.Value = ushort.Parse(newValue, NumberStyles.HexNumber);
                            break;
                        case 32:
                            register.Value = uint.Parse(newValue, NumberStyles.HexNumber);
                            break;
                        case 64:
                            register.Value = ulong.Parse(newValue, NumberStyles.HexNumber);
                            break;
                        default:
                            throw new NotSupportedException($"Invalid or unsupported register size {register.Size}.");
                    }
                    threadContext.Flush();
                }
            }

        }
    }
}