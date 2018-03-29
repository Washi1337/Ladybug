using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Ladybug.Core.Windows.Kernel32.Debug;
using Ladybug.Core.Windows.Kernel32.Debug.Events;

namespace Ladybug.Core.Windows.Kernel32
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet=CharSet.Auto, EntryPoint = "CreateProcess")]
        private static extern bool __CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes, 
            IntPtr lpThreadAttributes,
            bool bInheritHandles, 
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo, 
            out PROCESS_INFORMATION lpProcessInformation);

        public static PROCESS_INFORMATION CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes, 
            IntPtr lpThreadAttributes,
            bool bInheritHandles, 
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo)
        {
            if (!__CreateProcess(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes,
                bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, ref lpStartupInfo,
                out var processInfo))
            {
                throw new Win32Exception();
            }

            return processInfo;
        }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle,
            uint dwThreadId);
        
        [DllImport( "kernel32.dll", EntryPoint = "WaitForDebugEvent" )]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool __WaitForDebugEvent(ref DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        public static DEBUG_EVENT WaitForDebugEvent(uint dwMilliseconds)
        {
            var debugEvent = new DEBUG_EVENT();
            if (!__WaitForDebugEvent(ref debugEvent, dwMilliseconds))
                throw new Win32Exception();
            return debugEvent;
        }
        
        [DllImport("kernel32.dll", EntryPoint = "ContinueDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool __ContinueDebugEvent(uint dwProcessId, uint dwThreadId, ContinueStatus dwContinueStatus);

        public static void ContinueDebugEvent(uint dwProcessId, uint dwThreadId, ContinueStatus dwContinueStatus)
        {
            if (!__ContinueDebugEvent(dwProcessId, dwThreadId, dwContinueStatus))   
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
        static extern bool __ReadProcessMemory( 
            IntPtr hProcess, 
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, 
            int dwSize, 
            out IntPtr lpNumberOfBytesRead);

        public static void ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead)
        {
            if (!__ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, out lpNumberOfBytesRead))
                throw new Win32Exception();
        }
    }
}