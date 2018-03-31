using System;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    public class DebuggeeThread : IDebuggeeThread
    {
        private readonly IntPtr _threadHandle;

        internal DebuggeeThread(IDebuggeeProcess process, IntPtr threadHandle, int id, IntPtr startAddress)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            _threadHandle = threadHandle;
            Id = id;
            StartAddress = startAddress;
        }
        
        public IDebuggeeProcess Process
        {
            get;
        }

        public int Id
        {
            get;
        }

        public int ExitCode
        {
            get;
            internal set;
        }

        public IntPtr StartAddress
        {
            get;
            private set;
        }

        public IThreadContext GetThreadContext()
        {
            return new X86ThreadContext32(_threadHandle);
        }
    }
}