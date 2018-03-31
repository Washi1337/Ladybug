using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct M128A
    {
        public ulong High;
        public long Low;

        public override string ToString()
        {
            return string.Format("High:{0}, Low:{1}", this.High, this.Low);
        }
    }
}