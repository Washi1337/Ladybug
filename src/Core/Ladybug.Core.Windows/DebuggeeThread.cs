using System;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    public class DebuggeeThread : IDebuggeeThread
    {
        private readonly IntPtr _threadHandle;

        internal DebuggeeThread(DebuggeeProcess process, IntPtr threadHandle, int id, IntPtr startAddress)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            _threadHandle = threadHandle;
            Id = id;
            StartAddress = startAddress;
        }
        
        public DebuggeeProcess Process
        {
            get;
        }

        IDebuggeeProcess IDebuggeeThread.Process
        {
            get { return Process; }
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
            // TODO: determine whether a process is 32-bit or 64-bit.
            return new X86ThreadContext32(_threadHandle);
        }
    }
}