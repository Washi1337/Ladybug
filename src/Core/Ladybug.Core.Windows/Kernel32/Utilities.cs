using System;
using System.Text;
using Ladybug.Core.Windows.Kernel32.Debugging;

namespace Ladybug.Core.Windows.Kernel32
{
    internal static class Utilities
    {
        public static string ReadString(this IDebuggeeProcess process, IntPtr address, int length, bool unicode)
        {
            byte[] data = new byte[length];
            process.ReadMemory(address, data, 0, data.Length);
            var encoding = unicode ? Encoding.ASCII : Encoding.Unicode;
            return encoding.GetString(data);
        }
        
        public static ContinueStatus ToContinueStatus(this DebuggerAction nextAction)
        {
            switch (nextAction)
            {
                case DebuggerAction.Continue:
                    return ContinueStatus.DBG_CONTINUE;
                    break;
                case DebuggerAction.ContinueWithException:
                    return ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(nextAction));
            }
        }
    }
}