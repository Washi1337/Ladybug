using System;
using System.Collections.Generic;

namespace Ladybug.Core
{
    /// <summary>
    /// Represents a single process in a debugger session.
    /// </summary>
    public interface IDebuggeeProcess : IDisposable
    {
        event EventHandler<DebuggeeThreadEventArgs> ThreadStarted;
        event EventHandler<DebuggeeThreadEventArgs> ThreadTerminated;
        
        /// <summary>
        /// Gets the parent debugger session.
        /// </summary>
        IDebuggerSession Session
        {
            get;
        }

        /// <summary>
        /// Gets the unique identification number of the process.
        /// </summary>
        int Id
        {
            get;
        }

        /// <summary>
        /// Gets a collection of threads that are running in the process.
        /// </summary>
        ICollection<IDebuggeeThread> Threads
        {
            get;
        }

        /// <summary>
        /// Gets a collection of libraries that were loaded into the process.
        /// </summary>
        ICollection<IDebuggeeLibrary> Libraries
        {
            get;
        }

        /// <summary>
        /// When terminated, gets the exit code of the process.
        /// </summary>
        int ExitCode
        {
            get;
        }

        /// <summary>
        /// Gets the base memory address of the process.
        /// </summary>
        IntPtr BaseAddress
        {
            get;
        }
        
        /// <summary>
        /// Gets a thread by its unique identification number.
        /// </summary>
        /// <param name="id">The ID of the thread to get.</param>
        /// <returns>The thread with the given ID, or <c>null</c> if none was found.</returns>
        IDebuggeeThread GetThreadById(int id);
        
        /// <summary>
        /// Gets a library loaded at the given base address.
        /// </summary>
        /// <param name="baseAddress">The address of the library to get.</param>
        /// <returns>The library at the given address, or <c>null</c> if no library was mapped to that address.</returns>
        IDebuggeeLibrary GetLibraryByBase(IntPtr baseAddress);

        /// <summary>
        /// Breaks execution of the process.
        /// </summary>
        void Break();
        
        /// <summary>
        /// Gets a collection of user-defined breakpoints set in the code of the process.
        /// </summary>
        IEnumerable<IBreakpoint> GetSoftwareBreakpoints();

        /// <summary>
        /// Sets a new breakpoint at the given code address.
        /// </summary>
        /// <param name="address">The address to set the breakpoint at.</param>
        /// <returns>The breakpoint that was set.</returns>
        IBreakpoint SetSoftwareBreakpoint(IntPtr address);

        /// <summary>
        /// Removes a breakpoint from the process.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove.</param>
        void RemoveSoftwareBreakpoint(IBreakpoint breakpoint);

        /// <summary>
        /// Gets a user-defined breakpoint set in the process by its address.
        /// </summary>
        /// <param name="address">The address of the breakpoint.</param>
        /// <returns>The breakpoint at the given address, or <c>null</c> if none was found.</returns>
        IBreakpoint GetBreakpintByAddress(IntPtr address);
        
        /// <summary>
        /// Reads raw memory from the target process.
        /// </summary>
        /// <param name="address">The source address inside the process to read from.</param>
        /// <param name="buffer">The buffer to write the obtained memory to.</param>
        /// <param name="offset">The starting offset inside the buffer to write to.</param>
        /// <param name="length">The amount of bytes to read.</param>
        void ReadMemory(IntPtr address, byte[] buffer, int offset, int length);
        
        /// <summary>
        /// Writes raw memory to the target process.
        /// </summary>
        /// <param name="address">The destination address inside the process to write to.</param>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The starting offset inside the buffer to read from.</param>
        /// <param name="length">The amount of bytes to write.</param>
        void WriteMemory(IntPtr address, byte[] buffer, int offset, int length);
    }
}