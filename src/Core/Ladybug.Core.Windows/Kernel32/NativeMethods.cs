using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Ladybug.Core.Windows.Kernel32.Debugging;

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

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "TerminateProcess")]
        private static extern bool __TerminateProcess(IntPtr hProcess, uint exitCode);

        public static void TerminateProcess(IntPtr hProcess, uint exitCode)
        {
            if (!__TerminateProcess(hProcess, exitCode))
                throw new Win32Exception();
        }
            
        [DllImport("kernel32.dll", SetLastError = true,  EntryPoint = "WaitForDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool __WaitForDebugEvent(ref DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        public static DEBUG_EVENT WaitForDebugEvent(uint dwMilliseconds)
        {
            var debugEvent = new DEBUG_EVENT();
            if (!__WaitForDebugEvent(ref debugEvent, dwMilliseconds))
                throw new Win32Exception();
            return debugEvent;
        }
        
        [DllImport("kernel32.dll", SetLastError = true,  EntryPoint = "ContinueDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool __ContinueDebugEvent(uint dwProcessId, uint dwThreadId, ContinueStatus dwContinueStatus);

        public static void ContinueDebugEvent(uint dwProcessId, uint dwThreadId, ContinueStatus dwContinueStatus)
        {
            if (!__ContinueDebugEvent(dwProcessId, dwThreadId, dwContinueStatus))   
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true,  EntryPoint = "DebugBreakProcess")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool __DebugBreakProcess(IntPtr hProcess);

        public static void DebugBreakProcess(IntPtr hProcess)
        {
            if (!__DebugBreakProcess(hProcess))
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
        private static extern bool __ReadProcessMemory( 
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
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
        private static extern bool __WriteProcessMemory( 
            IntPtr hProcess, 
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, 
            int dwSize, 
            out IntPtr lpNumberOfBytesWritten);

        public static void WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesWritten)
        {
            if (!__WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, out lpNumberOfBytesWritten))
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "FlushInstructionCache")]
        private static extern bool __FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr dwSize);

        public static void FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr dwSize)
        {
            if (!__FlushInstructionCache(hProcess, lpBaseAddress, dwSize))
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "VirtualProtectEx")]
        private static extern bool __VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress,
            UIntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

        public static void VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress,
            UIntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect)
        {
            if (!__VirtualProtectEx(hProcess, lpAddress, dwSize, flNewProtect, out lpflOldProtect))
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetThreadContext")]
        private static extern bool __GetThreadContext(IntPtr hThread, ref CONTEXT lpContext);

        public static CONTEXT GetThreadContext32(IntPtr hThread)
        {
            var ctx = new CONTEXT();
            ctx.ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL;
            if (!__GetThreadContext(hThread, ref ctx))
                throw new Win32Exception();
            return ctx;
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetThreadContext")]
        private static extern bool __SetThreadContext(IntPtr hThread, [In] ref CONTEXT lpContext);

        public static void SetThreadContext32(IntPtr hThread, CONTEXT context)
        {
            if (!__SetThreadContext(hThread, ref context))
                throw new Win32Exception();
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetThreadContext")]
        private static extern bool __GetThreadContext(IntPtr hThread, ref CONTEXT64 lpContext);
                
        public static CONTEXT64 GetThreadContext64(IntPtr hThread)
        {
            var ctx = new CONTEXT64();
            ctx.ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL;
            if (!__GetThreadContext(hThread, ref ctx))
                throw new Win32Exception();
            return ctx;
        }
        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetThreadContext")]
        private static extern bool __SetThreadContext(IntPtr hThread, [In] ref CONTEXT64 lpContext);

        public static void SetThreadContext64(IntPtr hThread, CONTEXT64 context)
        {
            if (!__SetThreadContext(hThread, ref context))
                throw new Win32Exception();
        }
    }
}