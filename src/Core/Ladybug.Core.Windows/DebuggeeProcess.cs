using System;
using System.Collections.Generic;
using System.Linq;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    public class DebuggeeProcess : IDebuggeeProcess
    {
        public event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        public event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;

        private readonly IDictionary<int, IDebuggeeThread> _threads = new Dictionary<int, IDebuggeeThread>();
        private readonly IDictionary<IntPtr, IDebuggeeLibrary> _libraries = new Dictionary<IntPtr, IDebuggeeLibrary>();
        private readonly IDictionary<IntPtr, Int3Breakpoint> _int3Breakpoints = new Dictionary<IntPtr, Int3Breakpoint>();
        private readonly IDictionary<IntPtr, PageGuard> _pageGuards = new Dictionary<IntPtr, PageGuard>();
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

        /// <inheritdoc />
        public IDebuggerSession Session
        {
            get;
        }

        /// <inheritdoc />
        public int Id
        {
            get;
        }

        /// <inheritdoc />
        public ICollection<IDebuggeeThread> Threads
        {
            get { return _threads.Values; }
        }

        /// <inheritdoc />
        public ICollection<IDebuggeeLibrary> Libraries
        {
            get { return _libraries.Values; }
        }

        /// <inheritdoc />
        public int ExitCode
        {
            get;
            internal set;
        }

        /// <inheritdoc />
        public IntPtr BaseAddress
        {
            get;
            internal set;
        }

        /// <inheritdoc />
        public DebuggeeThread GetThreadById(int id)
        {
            _threads.TryGetValue(id, out var thread);
            return (DebuggeeThread) thread;
        }

        /// <inheritdoc />
        public DebuggeeLibrary GetLibraryByBase(IntPtr baseAddress)
        {
            _libraries.TryGetValue(baseAddress, out var library);
            return (DebuggeeLibrary) library;
        }

        /// <inheritdoc />
        public void Break()
        {
            NativeMethods.DebugBreakProcess(_processHandle);
        }

        /// <inheritdoc />
        public IEnumerable<ISoftwareBreakpoint> GetSoftwareBreakpoints()
        {
            return _int3Breakpoints.Values;
        }

        public Int3Breakpoint SetSoftwareBreakpoint(IntPtr address)
        {
            if (!_int3Breakpoints.TryGetValue(address, out var breakpoint))
            {
                breakpoint = new Int3Breakpoint(this, address, true);
                _int3Breakpoints[address] = breakpoint;
            }
            return breakpoint;
        }

        ISoftwareBreakpoint IDebuggeeProcess.SetSoftwareBreakpoint(IntPtr address)
        {
            return SetSoftwareBreakpoint(address);
        }

        public void RemoveSoftwareBreakpoint(Int3Breakpoint breakpoint)
        {
            if (breakpoint == null)
                throw new ArgumentNullException(nameof(breakpoint));
            
            if (_int3Breakpoints.Remove(breakpoint.Address))
                breakpoint.Enabled = false;
        }

        void IDebuggeeProcess.RemoveSoftwareBreakpoint(ISoftwareBreakpoint breakpoint)
        {
            
            RemoveSoftwareBreakpoint((Int3Breakpoint) breakpoint);
        }

        public Int3Breakpoint GetSoftwareBreakpointByAddress(IntPtr address)
        {
            _int3Breakpoints.TryGetValue(address, out var breakpoint);
            return breakpoint;
        }

        ISoftwareBreakpoint IDebuggeeProcess.GetSoftwareBreakpointByAddress(IntPtr address)
        {
            return GetSoftwareBreakpointByAddress(address);
        }

        public IEnumerable<IMemoryBreakpoint> GetMemoryBreakpoints()
        {
            return _pageGuards.Values.SelectMany(x => x.Breakpoints.Values);
        }

        public IMemoryBreakpoint SetMemoryBreakpoint(IntPtr address)
        {
            IntPtr pageAddress = GetPageAddress(address);
            if (!_pageGuards.TryGetValue(pageAddress, out var pageGuard))
            {
                pageGuard = new PageGuard(_processHandle, pageAddress);
                _pageGuards[pageAddress] = pageGuard;
            }

            if (!pageGuard.Breakpoints.TryGetValue(address, out var breakpoint))
            {
                breakpoint = new PageGuardBreakpoint(pageGuard, address, true);
                pageGuard.Breakpoints[address] = breakpoint;
            }
            
            pageGuard.Enabled = true;

            return breakpoint;
        }

        public void RemoveMemoryBreakpoint(IMemoryBreakpoint breakpoint)
        {
            throw new NotImplementedException();
        }

        public PageGuardBreakpoint GetMemoryBreakpointByAddress(IntPtr address)
        {
            IntPtr pageAddress = GetPageAddress(address);
            if (_pageGuards.TryGetValue(pageAddress, out var pageGuard)
                && pageGuard.Breakpoints.TryGetValue(address, out var breakpoint))
            {
                return breakpoint;
            }

            return null;
        }

        IMemoryBreakpoint IDebuggeeProcess.GetMemoryBreakpointByAddress(IntPtr address)
        {
            return GetMemoryBreakpointByAddress(address);
        }

        public void ReadMemory(IntPtr address, byte[] buffer, int offset, int length)
        {
            var tempBuffer = new byte[length];
            NativeMethods.ReadProcessMemory(_processHandle, address, tempBuffer, tempBuffer.Length, out var read);
            Buffer.BlockCopy(tempBuffer, 0, buffer, offset, length);
        }

        public void WriteMemory(IntPtr address, byte[] buffer, int offset, int length)
        {
            var tempBuffer = new byte[length];
            Buffer.BlockCopy(buffer, offset, tempBuffer, 0, length);
            NativeMethods.WriteProcessMemory(_processHandle, address, tempBuffer, tempBuffer.Length, out var written);
            
            // Code memory is loaded into the cache of the CPU. Make sure the changes appear in the cache too.
            NativeMethods.FlushInstructionCache(_processHandle, address, written);
        }

        public void Terminate()
        {
            NativeMethods.TerminateProcess(_processHandle, 0);
        }

        internal PageGuard GetContainingPageGuard(IntPtr address)
        {
            IntPtr pageAddress = GetPageAddress(address);
            _pageGuards.TryGetValue(pageAddress, out var pageGuard);
            return pageGuard;
        }

        private static IntPtr GetPageAddress(IntPtr address)
        {
            int pageSize = Environment.SystemPageSize;
            var pageAddress = (IntPtr) ((address.ToInt64() / pageSize) * pageSize);
            return pageAddress;
        }

        internal void AddThread(DebuggeeThread thread)
        {
            _threads.Add(thread.Id, thread);
            OnThreadStarted(new DebuggeeThreadEventArgs(thread));
        }

        internal void RemoveThread(DebuggeeThread thread)
        {
            _threads.Remove(thread.Id);
            OnThreadTerminated(new DebuggeeThreadEventArgs(thread));
        }

        internal void AddLibrary(DebuggeeLibrary library)
        {
            _libraries.Add(library.BaseOfLibrary, library);
        }

        internal void RemoveLibrary(DebuggeeLibrary library)
        {
            _libraries.Remove(library.BaseOfLibrary);
        }
        
        public void Dispose()
        {
        }

        protected virtual void OnThreadStarted(DebuggeeThreadEventArgs e)
        {
            ThreadStarted?.Invoke(this, e);
        }

        protected virtual void OnThreadTerminated(DebuggeeThreadEventArgs e)
        {
            ThreadTerminated?.Invoke(this, e);
        }

        IDebuggeeThread IDebuggeeProcess.GetThreadById(int id)
        {
            return GetThreadById(id);
        }

        IDebuggeeLibrary IDebuggeeProcess.GetLibraryByBase(IntPtr baseAddress)
        {
            return GetLibraryByBase(baseAddress);
        }
    }
}