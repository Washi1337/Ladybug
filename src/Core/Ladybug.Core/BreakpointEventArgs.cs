namespace Ladybug.Core
{
    public class BreakpointEventArgs : DebuggeeThreadEventArgs
    {
        public BreakpointEventArgs(IDebuggeeThread thread, IBreakpoint breakpoint) 
            : base(thread)
        {
            Breakpoint = breakpoint;
        }
        
        public IBreakpoint Breakpoint
        {
            get;
        }
    }
}