using System;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    public class DebuggeeThread : IDebuggeeThread
    {
        private readonly IntPtr _threadHandle;
        private IThreadContext _context;

        internal DebuggeeThread(IDebuggeeProcess process, IntPtr threadHandle, int id)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            _threadHandle = threadHandle;
            Id = id;
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

        public IThreadContext ThreadContext
        {
            get { return _context ?? (_context = new X86ThreadContext32(_threadHandle)); }
        }
    }
}