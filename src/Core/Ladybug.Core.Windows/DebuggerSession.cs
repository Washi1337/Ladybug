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
        public event EventHandler<DebuggeeExceptionEventArgs> ExceptionOccurred;
        public event EventHandler<DebuggeeThreadEventArgs> Stepped;
        public event EventHandler<DebuggeeThreadEventArgs> Paused;

        private readonly IDictionary<int, IDebuggeeProcess> _processes = new Dictionary<int, IDebuggeeProcess>();
        private readonly AutoResetEvent _continueEvent = new AutoResetEvent(false);
        private readonly IList<Int3Breakpoint> _pendingBreakpoint = new List<Int3Breakpoint>();
        private bool _isStepping = false;
        
        private ContinueStatus _nextContinueStatus;
        private DebuggeeThread _currentThread;
        private bool _isPaused;
        
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
            return _processes.Values.SelectMany(x => x.GetSoftwareBreakpoints());
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
            
            // Spawn debugger loop on new thread.
            new Thread(() =>
            {
                // Initialize startup info.
                var startupInfo = new STARTUPINFO();
                startupInfo.cb = (uint) Marshal.SizeOf<STARTUPINFO>();

                // Create process with debug flag set.
                var processInfo = NativeMethods.CreateProcess(
                    null, info.CommandLine, IntPtr.Zero, IntPtr.Zero, false,
                    ProcessCreationFlags.DEBUG_ONLY_THIS_PROCESS | ProcessCreationFlags.CREATE_NEW_CONSOLE,
                    IntPtr.Zero, null, ref startupInfo);

                process = GetOrCreateProcess(processInfo.hProcess, processInfo.dwProcessId);
                
                // Signal process has started.
                started.Set();
                
                // Enter debugger loop.
                DebuggerLoop();
            })
            {
                IsBackground = true
            }.Start();

            // Wait for process.
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

        private void RestoreBreakpoints()
        {
            foreach (var bp in _pendingBreakpoint.Where(x => x.Enabled))
                bp.InstallInt3();
            _pendingBreakpoint.Clear();
        }

        public void Continue(DebuggerAction nextAction)
        {
            if (_isPaused)
            {
                _isStepping = false;
                if (_pendingBreakpoint.Count > 0)
                    SignalSingleInstructionStep(nextAction);
                else
                    SignalDebuggerLoop(nextAction);
            }
        }

        public void Step(DebuggerAction nextAction)
        {
            if (_isPaused)
            {
                _isStepping = true;
                SignalSingleInstructionStep(nextAction);
            }
        }

        private void SignalSingleInstructionStep(DebuggerAction nextAction)
        {
            var threadContext = _currentThread.GetThreadContext();
            threadContext.GetRegisterByName("tf").Value = true;
            threadContext.Flush();
            SignalDebuggerLoop(nextAction);
        }

        private void SignalDebuggerLoop(DebuggerAction nextAction)
        {
            _nextContinueStatus = nextAction.ToContinueStatus();
            _continueEvent.Set();
        }

        private void DebuggerLoop()
        {
            while (IsActive)
            {
                // Handle next debugger event. 
                var nextEvent = NativeMethods.WaitForDebugEvent(uint.MaxValue);
                
                _isPaused = true;
                _currentThread = GetProcessById((int) nextEvent.dwProcessId)?.GetThreadById((int) nextEvent.dwThreadId);
                
                var nextAction = HandleDebugEvent(nextEvent);
                
                // Handle action that might have been set by subscribed event handlers.
                HandleNextAction(nextAction, nextEvent);
                
                // Continue execution.
                NativeMethods.ContinueDebugEvent(
                    nextEvent.dwProcessId, 
                    nextEvent.dwThreadId,
                    _nextContinueStatus);
                
                _isPaused = false;
            }    
        }

        private DebuggerAction HandleDebugEvent(DEBUG_EVENT nextEvent)
        {
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

            return nextAction;
        }

        private DebuggerAction HandleCreateProcessDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<CREATE_PROCESS_DEBUG_INFO>();
            var process = GetOrCreateProcess(info.hProcess, (int) debugEvent.dwProcessId);
            process.BaseAddress = info.lpBaseOfImage;
            
            // Create process event also spawns a new thread. 
            _currentThread = new DebuggeeThread(process, info.hThread, (int) debugEvent.dwThreadId, info.lpStartAddress);
            process.AddThread(_currentThread);
            
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
            
            var thread = new DebuggeeThread(process, info.hThread, (int) debugEvent.dwThreadId, info.lpStartAddress);
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

            var eventArgs = new DebuggeeOutputStringEventArgs(thread,
                process.ReadString(
                    info.lpDebugStringData, 
                    info.nDebugStringLength, 
                    info.fUnicode == 0));
            
            OnOutputStringSent(eventArgs);
            
            return eventArgs.NextAction;
        }

        private DebuggerAction HandleLoadDllDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<LOAD_DLL_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);

            // LOAD_DLL_DEBUG_INFO.lpImageName is a char** or a wchar_t**, which can be null.
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
                        name = process.ReadZeroTerminatedString(ptr, info.fUnicode == 0);
                    }
                }
            }
            catch (Win32Exception)
            {
                // Reading failed, possibly due to an invalid pointer address. Set to no name instead.
                name = null;
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

        private DebuggerAction HandleExceptionDebugEvent(DEBUG_EVENT debugEvent)
        {
            var info = debugEvent.InterpretDebugInfoAs<EXCEPTION_DEBUG_INFO>();
            var process = GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);

            var nextAction = DebuggerAction.Stop;
            
            switch (info.ExceptionRecord.ExceptionCode)
            {
                case ExceptionCode.EXCEPTION_BREAKPOINT:
                {
                    // If signalled by an int3, the exception was thrown after the execution of int3.
                    // Find corresponding breakpoint and restore the instruction pointer so that it seems
                    // it has paused execution before the int3.

                    uint eip = (uint) thread.GetThreadContext().GetRegisterByName("eip").Value - 1;
                    var breakpoint = process.GetBreakpointByAddress((IntPtr) eip);
                    if (breakpoint != null)
                    {
                        var eventArgs = new BreakpointEventArgs(thread, breakpoint);
                        breakpoint.HandleBreakpointEvent(eventArgs);
                        OnBreakpointHit(eventArgs);
                        _pendingBreakpoint.Add(breakpoint);
                    }

                    break;
                }
                
                case ExceptionCode.EXCEPTION_SINGLE_STEP:
                {
                    if (_pendingBreakpoint.Count > 0)
                    {
                        RestoreBreakpoints();
                        if (!_isStepping)
                        {
                            nextAction = DebuggerAction.Continue;
                            break;
                        }
                    }
                    
                    var eventArgs = new DebuggeeThreadEventArgs(thread)
                    {
                        NextAction = DebuggerAction.Stop
                    };
                    OnStepped(eventArgs);
                    nextAction = eventArgs.NextAction;
                    break;
                }

                default:
                {
                    // Forward exception to debugger.
                    var eventArgs = new DebuggeeExceptionEventArgs(thread,
                        new DebuggeeException((uint) info.ExceptionRecord.ExceptionCode,
                            info.ExceptionRecord.ExceptionCode.ToString(),
                            info.dwFirstChance == 1,
                            info.ExceptionRecord.ExceptionFlags == 0));
                    OnExceptionOccurred(eventArgs);
                    nextAction = eventArgs.NextAction;
                    break;
                }
            }
            
            return nextAction;
        }

        private void HandleNextAction(DebuggerAction nextAction, DEBUG_EVENT nextEvent)
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
                    var process = GetProcessById((int) nextEvent.dwProcessId);
                    var thread = process.GetThreadById((int) nextEvent.dwThreadId)
                                 ?? new DebuggeeThread(process, IntPtr.Zero, (int) nextEvent.dwThreadId, IntPtr.Zero);
                
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

        protected virtual void OnExceptionOccurred(DebuggeeExceptionEventArgs e)
        {
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnPaused(DebuggeeThreadEventArgs e)
        {
            Paused?.Invoke(this, e);
        }

        protected virtual void OnBreakpointHit(BreakpointEventArgs e)
        {
            BreakpointHit?.Invoke(this, e);
        }

        protected virtual void OnStepped(DebuggeeThreadEventArgs e)
        {
            Stepped?.Invoke(this, e);
        }
    }
}