namespace Ladybug.Core
{
    /// <summary>
    /// Provides valid actions that can be taken when continuing execution.
    /// </summary>
    public enum DebuggerAction
    {
        /// <summary>
        /// Continue execution, consuming any exceptions that the debuggee might have thrown.  
        /// </summary>
        Continue,
        
        /// <summary>
        /// Continue execution, but pass all thrown exceptions back to the debuggee. 
        /// </summary>
        ContinueWithException,
        
        /// <summary>
        /// Do not continue execution.
        /// </summary>
        Stop,
    }
}