using System;
using System.Collections.Generic;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    public class DebuggeeProcess : IDebuggeeProcess
    {
        public event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        public event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;

        private readonly IDictionary<int, IDebuggeeThread> _threads = new Dictionary<int, IDebuggeeThread>();
        private readonly IntPtr _processHandle;

        internal DebuggeeProcess(IDebuggerSession session, PROCESS_INFORMATION processInfo)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _processHandle = processInfo.hProcess;
            Id = processInfo.dwProcessId;
        }

        internal DebuggeeProcess(IDebuggerSession session, IntPtr handle, int id)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _processHandle = handle;
            Id = id;
        }

        public IDebuggerSession Session
        {
            get;
        }

        public int Id
        {
            get;
        }

        public ICollection<IDebuggeeThread> Threads
        {
            get { return _threads.Values; }
        }

        public int ExitCode
        {
            get;
            internal set;
        }

        public IEnumerable<IBreakpoint> GetAllBreakpoints()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SoftwareBreakpoint> GetSoftwareBreakpoints()
        {
            throw new NotImplementedException();
        }

        public SoftwareBreakpoint SetSoftwareBreakpoint(IntPtr address)
        {
            throw new NotImplementedException();
        }

        public void RemoveSoftwareBreakpoint(SoftwareBreakpoint breakpoint)
        {
            throw new NotImplementedException();
        }

        public void ReadMemory(IntPtr address, byte[] buffer, int offset, int length)
        {
            byte[] b = new byte[length];
            NativeMethods.ReadProcessMemory(_processHandle, address, b, b.Length, out var read);
            Buffer.BlockCopy(b, 0, buffer, offset, length);
        }

        public void WriteMemory(IntPtr address, byte[] buffer, int offset, int length)
        {
            throw new NotImplementedException();
        }

        internal void AddThread(DebuggeeThread thread)
        {
            _threads.Add(thread.Id, thread);
        }

        internal void RemoveThread(DebuggeeThread thread)
        {
            _threads.Remove(thread.Id);
        }
        
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        internal virtual void OnThreadStarted(DebuggeeThreadEventArgs e)
        {
            ThreadStarted?.Invoke(this, e);
        }

        internal virtual void OnThreadTerminated(DebuggeeThreadEventArgs e)
        {
            ThreadTerminated?.Invoke(this, e);
        }

        public DebuggeeThread GetThreadById(int id)
        {
            _threads.TryGetValue(id, out var thread);
            return (DebuggeeThread) thread;
        }

        IDebuggeeThread IDebuggeeProcess.GetThreadById(int id)
        {
            return GetThreadById(id);
        }
    }
}