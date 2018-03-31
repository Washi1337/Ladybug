using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Ladybug.Core.Windows.Kernel32;
using Ladybug.Core.Windows.Kernel32.Debugging;

namespace Ladybug.Core.Windows
{
    public class DebuggerSession : IDebuggerSession
    {
        public event EventHandler<DebuggeeProcessEventArgs> ProcessStarted;
        public event EventHandler<DebuggeeProcessEventArgs> ProcessTerminated;
        public event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        public event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;
        public event EventHandler<DebuggeeLibraryEventArgs> LibraryLoaded;
        public event EventHandler<DebuggeeLibraryEventArgs> LibraryUnloaded;
        public event EventHandler<BreakpointEventArgs> BreakpointHit;
        public event EventHandler<DebuggeeOutputStringEventArgs> OutputStringSent;
        public event EventHandler<DebuggeeThreadEventArgs> Paused;

        private readonly IDictionary<int, IDebuggeeProcess> _processes = new Dictionary<int, IDebuggeeProcess>();
        private ContinueStatus _nextContinueStatus;
        private readonly AutoResetEvent _continueEvent = new AutoResetEvent(false);
        
        public DebuggerSession()
        {
        }
        
        ~DebuggerSession()
        {
            ReleaseUnmanagedResources();
        }

        public bool IsActive
        {
            get;
            private set;
        }

        public IEnumerable<IDebuggeeProcess> GetProcesses()
        {
            return _processes.Values;
        }

        public IEnumerable<IBreakpoint> GetAllBreakpoints()
        {
            return _processes.Values.SelectMany(x => x.GetAllBreakpoints());
        }

        public DebuggeeProcess GetProcessById(int id)
        {
            _processes.TryGetValue(id, out var process);
            return (DebuggeeProcess) process;
        }

        IDebuggeeProcess IDebuggerSession.GetProcessById(int id)
        {
            return GetProcessById(id);
        }

        private DebuggeeProcess GetOrCreateProcess(IntPtr processHandle, int processId)
        {
            lock (_processes)
            {
                var process = GetProcessById(processId);
                if (process == null)
                {
                    process = new DebuggeeProcess(this, processHandle, processId);
                    _processes.Add(processId, process);
                }
                return process;
            }
        }

        public DebuggeeProcess StartProcess(DebuggerProcessStartInfo info)
        {
            IsActive = true;
            var started = new ManualResetEvent(false);
            DebuggeeProcess process = null;
            new Thread(() =>
            {
                var startupInfo = new STARTUPINFO();
                startupInfo.cb = (uint) Marshal.SizeOf<STARTUPINFO>();

                var processInfo = NativeMethods.CreateProcess(
                    null, info.CommandLine, IntPtr.Zero, IntPtr.Zero, false,
                    ProcessCreationFlags.DEBUG_ONLY_THIS_PROCESS | ProcessCreationFlags.CREATE_NEW_CONSOLE,
                    IntPtr.Zero, null, ref startupInfo);

                process = GetOrCreateProcess(processInfo.hProcess, processInfo.dwProcessId);
                started.Set();
                
                DebuggerLoop();
            })
            {
                IsBackground = true
            }.Start();

            started.WaitOne();
            return process;
        }

        IDebuggeeProcess IDebuggerSession.StartProcess(DebuggerProcessStartInfo info)
        {
            return StartProcess(info);
        }
        
        public DebuggeeProcess AttachToProcess(int processId)
        {
            throw new System.NotImplementedException();
        }

        IDebuggeeProcess IDebuggerSession.AttachToProcess(int processId)
        {
            return AttachToProcess(processId);
        }

        public void Continue(DebuggerAction nextAction)
        {
            switch (nextAction)
            {
                case DebuggerAction.Continue:
                    _nextContinueStatus = ContinueStatus.DBG_CONTINUE;
                    break;
                case DebuggerAction.ContinueWithException:
                    _nextContinueStatus = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                    break;
                case DebuggerAction.Stop:
                    throw new ArgumentOutOfRangeException(nameof(nextAction));
            }
            _continueEvent.Set();
        }

        private void DebuggerLoop()
        {
            while (IsActive)
            {
                var nextEvent = NativeMethods.WaitForDebugEvent(uint.MaxValue);
                var nextAction = DebuggerAction.Continue;

                switch (nextEvent.dwDebugEventCode)
                {
                    case DebugEventCode.CREATE_PROCESS_DEBUG_EVENT:
                        nextAction = HandleCreateProcessDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.EXIT_PROCESS_DEBUG_EVENT:
                        nextAction = HandleExitProcessDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.OUTPUT_DEBUG_STRING_EVENT:
                        nextAction = HandleOutputStringDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.CREATE_THREAD_DEBUG_EVENT:
                        nextAction = HandleCreateThreadDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.EXCEPTION_DEBUG_EVENT:
                        nextAction = HandleExceptionDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.EXIT_THREAD_DEBUG_EVENT:
                        nextAction = HandleExitThreadDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.LOAD_DLL_DEBUG_EVENT:
                        nextAction = HandleLoadDllDebugEvent(nextEvent);
                        break;
                    case DebugEventCode.RIP_EVENT:
                        break;
                    case DebugEventCode.UNLOAD_DLL_DEBUG_EVENT:
                        nextAction = HandleUnloadDllDebugEvent(nextEvent);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (nextAction)
                {
                    case DebuggerAction.Continue:
                        _nextContinueStatus = ContinueStatus.DBG_CONTINUE;
                        break;
                    case DebuggerAction.ContinueWithException:
                        _nextContinueStatus = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                        break;
                    case DebuggerAction.Stop:
                        var process = GetProcessById((int) nextEvent.dwProcessId);
                        var thread = process.GetThreadById((int) nextEvent.dwThreadId)
                                     ?? new DebuggeeThread(process, IntPtr.Zero, (int) nextEvent.dwThreadId);
                        var eventArgs = new DebuggeeThreadEventArgs(thread);
                        eventArgs.NextAction = DebuggerAction.Stop;
                        OnPaused(eventArgs);
                        switch (eventArgs.NextAction)
                        {
                            case DebuggerAction.Continue:
                                _nextContinueStatus = ContinueStatus.DBG_CONTINUE;
                                break;
                            case DebuggerAction.ContinueWithException:
                                _nextContinueStatus = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                                break;
                            case DebuggerAction.Stop:
                                _continueEvent.WaitOne();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                }
                
                NativeMethods.ContinueDebugEvent(
                    nextEvent.dwProcessId, 
                    nextEvent.dwThreadId,
                    _nextContinueStatus);
            }    
        }

        private DebuggerAction HandleCreateProcessDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<CREATE_PROCESS_DEBUG_INFO>();
            var process = GetOrCreateProcess(info.hProcess, (int) debugEvent.dwProcessId);
            
            var thread = new DebuggeeThread(process, info.hThread, (int) debugEvent.dwThreadId);
            process.AddThread(thread);
            
            var eventArgs = new DebuggeeProcessEventArgs(process);
            OnProcessStarted(eventArgs);
            
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleExitProcessDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<EXIT_PROCESS_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);

            process.ExitCode = (int) info.dwExitCode;
            
            var eventArgs = new DebuggeeProcessEventArgs(process);
            OnProcessTerminated(eventArgs);
            
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleCreateThreadDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<CREATE_THREAD_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            
            var thread = new DebuggeeThread(process, info.hThread, (int) debugEvent.dwThreadId);
            process.AddThread(thread);
            
            var eventArgs = new DebuggeeThreadEventArgs(thread);
            OnThreadStarted(eventArgs);
            
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleExitThreadDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<EXIT_THREAD_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);
            
            thread.ExitCode = (int) info.dwExitCode;
            
            var eventArgs = new DebuggeeThreadEventArgs(thread);
            OnThreadTerminated(eventArgs);
            
            process.RemoveThread(thread);
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleOutputStringDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<OUTPUT_DEBUG_STRING_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);
            
            var eventArgs = new DebuggeeOutputStringEventArgs(thread, process.ReadString(info.lpDebugStringData, info.nDebugStringLength, info.fUnicode == 0));
            OnOutputStringSent(eventArgs);
            
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleLoadDllDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<LOAD_DLL_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);

            string name = null;
            try
            {
                if (info.lpImageName != IntPtr.Zero)
                {
                    var buffer = new byte[8];
                    process.ReadMemory(info.lpImageName, buffer, 0, IntPtr.Size);
                    var ptr = new IntPtr(BitConverter.ToInt64(buffer, 0));

                    if (ptr != IntPtr.Zero)
                    {
                        name = process.ReadString(ptr, 255, info.fUnicode == 0);
                        name = name.Remove(name.IndexOf('\0'));
                    }
                }
            }
            catch (Win32Exception)
            {
            }

            var library = new DebuggeeLibrary(process, name, info.lpBaseOfDll);
            process.AddLibrary(library);
            
            var eventArgs = new DebuggeeLibraryEventArgs(thread, library);
            OnLibraryLoaded(eventArgs);
            
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleUnloadDllDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<UNLOAD_DLL_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);
            var library = process.GetLibraryByBase(info.lpBaseOfDll);

            if (library != null)
            {
                process.RemoveLibrary(library);

                var eventArgs = new DebuggeeLibraryEventArgs(thread, library);
                OnLibraryUnloaded(eventArgs);
                
                return eventArgs.NextAction;
            }

            return DebuggerAction.Continue;
        }

        private DebuggerAction HandleExceptionDebugEvent(DEBUG_EVENT nextEvent)
        {
            var info = nextEvent.InterpretDebugInfoAs<EXCEPTION_DEBUG_INFO>();

            switch (info.ExceptionRecord.ExceptionCode)
            {
                case ExceptionCode.EXCEPTION_BREAKPOINT:
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return DebuggerAction.Stop;
        }

        private void ReleaseUnmanagedResources()
        {
            foreach (var process in _processes.Values)
                process.Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        protected virtual void OnProcessStarted(DebuggeeProcessEventArgs e)
        {
            ProcessStarted?.Invoke(this, e);
        }

        protected virtual void OnProcessTerminated(DebuggeeProcessEventArgs e)
        {
            ProcessTerminated?.Invoke(this, e);
        }

        protected virtual void OnThreadStarted(DebuggeeThreadEventArgs e)
        {
            ThreadStarted?.Invoke(this, e);
        }

        protected virtual void OnThreadTerminated(DebuggeeThreadEventArgs e)
        {
            ThreadTerminated?.Invoke(this, e);
        }

        protected virtual void OnPaused(DebuggeeThreadEventArgs e)
        {
            Paused?.Invoke(this, e);
        }

        protected virtual void OnOutputStringSent(DebuggeeOutputStringEventArgs e)
        {
            OutputStringSent?.Invoke(this, e);
        }

        protected virtual void OnLibraryLoaded(DebuggeeLibraryEventArgs e)
        {
            LibraryLoaded?.Invoke(this, e);
        }

        protected virtual void OnLibraryUnloaded(DebuggeeLibraryEventArgs e)
        {
            LibraryUnloaded?.Invoke(this, e);
        }
    }
}