using System;

namespace Ladybug.Core
{
    /// <summary>
    /// Represents a single library loaded into a process.
    /// </summary>
    public interface IDebuggeeLibrary
    {
        /// <summary>
        /// Gets the process the library was loaded into.
        /// </summary>
        IDebuggeeProcess Process
        {
            get;
        }
        
        /// <summary>
        /// Gets the name of the library that was loaded.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the address the library was mapped to.
        /// </summary>
        IntPtr BaseOfLibrary
        {
            get;
        }
    }
}