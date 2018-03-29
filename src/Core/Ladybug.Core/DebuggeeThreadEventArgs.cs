namespace Ladybug.Core
{
    public class DebuggeeThreadEventArgs : DebuggerSessionEventArgs
    {
        public DebuggeeThreadEventArgs(IDebuggeeThread thread)
            : base(thread.Process.Session)
        {
            Thread = thread;
        }
        
        public IDebuggeeThread Thread
        {
            get;
        }
    }
}