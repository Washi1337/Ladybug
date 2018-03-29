using System;
using System.Collections.Generic;

namespace Ladybug.Core
{
    public interface IDebuggeeProcess : IDisposable
    {
        event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;
        
        IDebuggerSession Session
        {
            get;
        }

        int Id
        {
            get;
        }

        ICollection<IDebuggeeThread> Threads
        {
            get;
        }

        ICollection<IDebuggeeLibrary> Libraries
        {
            get;
        }

        int ExitCode
        {
            get;
        }
        
        IDebuggeeThread GetThreadById(int id);
        
        IDebuggeeLibrary GetLibraryByBase(IntPtr baseAddress);

        IEnumerable<IBreakpoint> GetAllBreakpoints();
        
        IEnumerable<SoftwareBreakpoint> GetSoftwareBreakpoints();

        SoftwareBreakpoint SetSoftwareBreakpoint(IntPtr address);

        void RemoveSoftwareBreakpoint(SoftwareBreakpoint breakpoint);
        
        void ReadMemory(IntPtr address, byte[] buffer, int offset, int length);
        
        void WriteMemory(IntPtr address, byte[] buffer, int offset, int length);
    }
}