namespace Ladybug.Core
{
    public class BreakpointEventArgs : DebuggerSessionEventArgs
    {
        public BreakpointEventArgs(IDebuggerSession session, IBreakpoint breakpoint)
            : base(session)
        {
            Breakpoint = breakpoint;
        }
        
        public IBreakpoint Breakpoint
        {
            get;
        }
    }
}