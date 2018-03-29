using System;
using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }
}