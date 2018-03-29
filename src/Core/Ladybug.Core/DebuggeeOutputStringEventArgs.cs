namespace Ladybug.Core
{
    public class DebuggeeOutputStringEventArgs : DebuggeeThreadEventArgs
    {
        public DebuggeeOutputStringEventArgs(IDebuggeeThread thread, string message)
            : base(thread)
        {
            Message = message;
        }
        
        public string Message
        {
            get;
        } 
    }
}