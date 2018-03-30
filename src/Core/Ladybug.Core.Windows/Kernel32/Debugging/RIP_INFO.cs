using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32.Debugging
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RIP_INFO
    {
        public uint dwError;
        public uint dwType;
    }
}