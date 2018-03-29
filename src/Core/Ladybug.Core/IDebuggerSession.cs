using System;
using System.Collections.Generic;

namespace Ladybug.Core
{
    public interface IDebuggerSession : IDisposable
    {
        event EventHandler<DebuggeeProcessEventArgs> ProcessStarted;
        event EventHandler<DebuggeeProcessEventArgs> ProcessTerminated;
        event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;
        event EventHandler<DebuggeeLibraryEventArgs> LibraryLoaded;
        event EventHandler<DebuggeeLibraryEventArgs> LibraryUnloaded;
        event EventHandler<BreakpointEventArgs> BreakpointHit;
        event EventHandler<DebuggeeOutputStringEventArgs> OutputStringSent;
        event EventHandler<DebuggeeThreadEventArgs> Paused;
        
        bool IsActive
        {
            get;
        }

        IEnumerable<IDebuggeeProcess> GetProcesses();

        IEnumerable<IBreakpoint> GetAllBreakpoints();
        
        IDebuggeeProcess GetProcessById(int id);

        IDebuggeeProcess StartProcess(DebuggerProcessStartInfo info);

        IDebuggeeProcess AttachToProcess(int processId);

        void Continue(DebuggerAction nextAction);
    }
}