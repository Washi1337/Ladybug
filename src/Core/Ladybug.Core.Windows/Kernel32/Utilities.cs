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

        public static string ReadZeroTerminatedString(this IDebuggeeProcess process, IntPtr address, bool unicode, int maxLength = 0x100)
        {
            // We do not know the size of the string, therefore read the memory in chunks and search for a zero byte.
            
            const int bufferSize = 256;
            var encoding = unicode ? Encoding.ASCII : Encoding.Unicode;
            var builder = new StringBuilder();

            IntPtr currentAddress = address;
            byte[] buffer = new byte[bufferSize];
            string lastChunk;
            int nullIndex;
            
            do
            {
                process.ReadMemory(currentAddress, buffer, 0, buffer.Length);
                lastChunk = encoding.GetString(buffer);
                nullIndex = lastChunk.IndexOf('\0');    
                builder.Append(lastChunk);
                currentAddress += bufferSize;
            } while (nullIndex == -1 && builder.Length < 0x100);

            // If \0 was found, remove everything after this character. Otherwise just return the raw string.
            string rawString = builder.ToString();
            return nullIndex != -1 
                ? rawString.Remove(builder.Length - (lastChunk.Length - nullIndex))
                : rawString;
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