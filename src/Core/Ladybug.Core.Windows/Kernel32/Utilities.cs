using System;
using System.Text;

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
    }
}