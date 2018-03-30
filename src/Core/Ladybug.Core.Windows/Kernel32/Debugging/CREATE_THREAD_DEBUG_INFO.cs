﻿using System;
using System.Runtime.InteropServices;

namespace Ladybug.Core.Windows.Kernel32.Debugging
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CREATE_THREAD_DEBUG_INFO
    {
        public IntPtr hThread;
        public IntPtr lpThreadLocalBase;
        public IntPtr lpStartAddress;
    }
}