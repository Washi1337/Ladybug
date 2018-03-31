using System;

namespace Ladybug.Core
{
    /// <summary>
    /// Represents a single thread inside a debuggee process.
    /// </summary>
    public interface IDebuggeeThread
    {
        /// <summary>
        /// Gets the process that the thread is running in.
        /// </summary>
        IDebuggeeProcess Process
        {
            get;
        }
        
        /// <summary>
        /// Gets the unique identification number of the thread.
        /// </summary>
        int Id
        {
            get;
        }

        /// <summary>
        /// When terminated, gets the exit code of the thread.
        /// </summary>
        int ExitCode
        {
            get;
        }

        /// <summary>
        /// Gets the starting address of the thread.
        /// </summary>
        IntPtr StartAddress
        {
            get;
        }

        /// <summary>
        /// Makes a snapshot of the thread context, containing values such as register values.
        /// </summary>
        /// <returns>The created snapshot of the thread context.</returns>
        IThreadContext GetThreadContext();
    }
}