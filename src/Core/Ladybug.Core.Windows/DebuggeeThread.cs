using System;

namespace Ladybug.Core.Windows
{
    public class DebuggeeThread : IDebuggeeThread
    {
        private readonly IntPtr _threadHandle;

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
    }
}