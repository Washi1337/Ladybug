using System;

namespace Ladybug.Core
{
    public interface IBreakpoint
    {
        event EventHandler<BreakpointEventArgs> BreakpointHit;
        
        bool Enabled
        {
            get;
            set;
        }
        
        
    }
}