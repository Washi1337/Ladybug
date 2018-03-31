namespace Ladybug.Core
{
    public class DebuggeeExceptionEventArgs : DebuggeeThreadEventArgs
    {
        public DebuggeeExceptionEventArgs(IDebuggeeThread thread, DebuggeeException exception) 
            : base(thread)
        {
            Exception = exception;
            NextAction = DebuggerAction.Stop;
        }
        
        public DebuggeeException Exception
        {
            get;
        }
    }
}