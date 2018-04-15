using System;
using System.Collections.Generic;

namespace Ladybug.Core
{
    /// <summary>
    /// Provides members for debugging executables.
    /// </summary>
    public interface IDebuggerSession : IDisposable
    {
        /// <summary>
        /// Occurs when a new process has started.
        /// </summary>
        event EventHandler<DebuggeeProcessEventArgs> ProcessStarted;
        
        /// <summary>
        /// Occurs when a process that was started during the session has terminated. 
        /// </summary>
        event EventHandler<DebuggeeProcessEventArgs> ProcessTerminated;
        
        /// <summary>
        /// Occurs when a new thread was started in a debuggee process.
        /// </summary>
        event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        
        /// <summary>
        /// Occurs when a thread inside a debuggee process has terminated.
        /// </summary>
        event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;
        
        /// <summary>
        /// Occurs when a library was loaded into the memory of a debuggee process. 
        /// </summary>
        event EventHandler<DebuggeeLibraryEventArgs> LibraryLoaded;
        
        /// <summary>
        /// Occurs when a library was unloaded from memory of a debuggee process.
        /// </summary>
        event EventHandler<DebuggeeLibraryEventArgs> LibraryUnloaded;
        
        /// <summary>
        /// Occurs when a user-defined breakpoint was hit during the execution of a debuggee process.
        /// </summary>
        event EventHandler<BreakpointEventArgs> BreakpointHit;
        
        /// <summary>
        /// Occurs when a debuggee process has sent a debugging message to the debugger. 
        /// </summary>
        event EventHandler<DebuggeeOutputStringEventArgs> OutputStringSent;
        
        /// <summary>
        /// Occurs when an exception has occurred in a debuggee process.
        /// </summary>
        event EventHandler<DebuggeeExceptionEventArgs> ExceptionOccurred;
        
        /// <summary>
        /// Occurs when a debuggee process has stepped one instruction.
        /// </summary>
        event EventHandler<DebuggeeThreadEventArgs> Stepped;
        
        /// <summary>
        /// Occurs when a debuggee process has paused execution.
        /// </summary>
        event EventHandler<DebuggeeThreadEventArgs> Paused;
        
        /// <summary>
        /// Gets a value indicating whether the session is currently active or not.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Gets a collection of all debugged processes in the session.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDebuggeeProcess> GetProcesses();

        /// <summary>
        /// Gets a collection of all set breakpoints in the session.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IBreakpoint> GetAllBreakpoints();
        
        /// <summary>
        /// Gets a debugged process by its unique identifaction number.
        /// </summary>
        /// <param name="id">The ID of the process to get.</param>
        /// <returns>The process, or <c>null</c> if none can be found.</returns>
        IDebuggeeProcess GetProcessById(int id);

        /// <summary>
        /// Starts debugging a new process using the given start information.
        /// </summary>
        /// <param name="info">The starting parameters to use for starting the executable.</param>
        /// <returns>The process that is being debugged.</returns>
        IDebuggeeProcess StartProcess(DebuggerProcessStartInfo info);

        /// <summary>
        /// Attaches the debugger to an already running process.
        /// </summary>
        /// <param name="processId">The ID of the process to attach to.</param>
        /// <returns>The process that the debugger attached to.</returns>
        IDebuggeeProcess AttachToProcess(int processId);
        
        /// <summary>
        /// Continues execution of a paused debuggee process.
        /// </summary>
        /// <param name="nextAction">The action to perform.</param>
        void Continue(DebuggerAction nextAction);

        /// <summary>
        /// Performs a step in the paused debuggee process.
        /// </summary>
        /// <param name="stepType">The type of step to take.</param>
        /// <param name="nextAction">The action to perform.</param>
        void Step(StepType stepType, DebuggerAction nextAction);
    }
}