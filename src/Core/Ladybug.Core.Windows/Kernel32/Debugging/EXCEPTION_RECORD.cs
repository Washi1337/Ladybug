using System;
using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32.Debugging
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EXCEPTION_RECORD
    {
        public ExceptionCode ExceptionCode;
        public uint ExceptionFlags;
        public IntPtr ExceptionRecord;
        public IntPtr ExceptionAddress;
        public uint NumberParameters;
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.U4 )]
        public uint[] ExceptionInformation;
    }
}