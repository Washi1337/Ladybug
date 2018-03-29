using System;

namespace Ladybug.Core
{
    public abstract class DebuggerSessionEventArgs : EventArgs
    {
        protected DebuggerSessionEventArgs(IDebuggerSession session)
        {
            Session = session;
            NextAction = DebuggerAction.Continue;
        }
        
        public IDebuggerSession Session
        {
            get;
        }

        public DebuggerAction NextAction
        {
            get;
            set;
        }
    }
}