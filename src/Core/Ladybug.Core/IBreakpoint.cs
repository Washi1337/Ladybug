using System;

namespace Ladybug.Core
{
    /// <summary>
    /// Represents a user-defined breakpoint set in a debuggee process.
    /// </summary>
    public interface IBreakpoint
    {
        /// <summary>
        /// Occurs when the breakpoint was hit.
        /// </summary>
        event EventHandler<BreakpointEventArgs> BreakpointHit;
        
        /// <summary>
        /// Gets or sets a value indicating whether the breakpoint is enabled or not.
        /// </summary>
        bool Enabled
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets the address the breakpoint was set at.
        /// </summary>
        IntPtr Address
        {
            get;
        }
    }
}