using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32.Debugging
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DEBUG_EVENT
    {
        public DebugEventCode dwDebugEventCode;
        public uint dwProcessId;
        public uint dwThreadId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
        byte[] debugInfo;
        
        public T InterpretDebugInfoAs<T>() 
            where T : struct
        {
            var structSize = Marshal.SizeOf(typeof(T));
            var pointer = Marshal.AllocHGlobal(structSize);
            Marshal.Copy(debugInfo, 0, pointer, structSize);

            var result = Marshal.PtrToStructure(pointer, typeof(T));
            Marshal.FreeHGlobal(pointer);
            return (T)result;
        }
    }
}