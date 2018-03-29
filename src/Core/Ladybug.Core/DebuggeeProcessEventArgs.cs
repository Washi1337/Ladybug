namespace Ladybug.Core
{
    public class DebuggeeProcessEventArgs : DebuggerSessionEventArgs
    {
        public DebuggeeProcessEventArgs(IDebuggeeProcess process)
            : base(process.Session)
        {
            Process = process;
        }
        
        public IDebuggeeProcess Process
        {
            get;
        }
    }
}