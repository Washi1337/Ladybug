﻿using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32.Debugging
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EXIT_PROCESS_DEBUG_INFO
    {
        public uint dwExitCode;
    }
}