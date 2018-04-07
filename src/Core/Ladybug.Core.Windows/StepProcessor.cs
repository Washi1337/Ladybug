using System;
using System.Collections.Generic;
using System.Linq;

namespace Ladybug.Core.Windows
{
    internal class StepProcessor
    {
        public event EventHandler<DebuggeeThreadEventArgs> ResumedExecution;
        public event EventHandler<DebuggeeThreadEventArgs> StepCompleted;
        
        private readonly DebuggerSession _session;
        private readonly IList<Int3Breakpoint> _breakpointsToRestore = new List<Int3Breakpoint>();
        private DebuggerAction _continueAction;
            
        public StepProcessor(DebuggerSession session)
        {
            _session = session;
        }

        public bool HasBreakpointsToRestore
        {
            get { return _breakpointsToRestore.Count > 0; }
        }

        public bool IsStepping
        {
            get;
            private set;
        }

        public bool IsContinuing
        {
            get;
            private set;
        }

        public void Continue(DebuggeeThread thread, DebuggerAction nextAction)
        {
            IsContinuing = true;
            _continueAction = nextAction;

            if (HasBreakpointsToRestore)
                SignalStep(thread, StepType.StepIn, nextAction);
            else
                _session.SignalDebuggerLoop(FinalizeContinue(thread));
        }

        public void SignalStep(DebuggeeThread thread, StepType stepType, DebuggerAction nextAction)
        {
            IsStepping = true;
            switch (stepType)
            {
                case StepType.StepIn:
                    SignalStepIn(thread, nextAction);
                    break;
                case StepType.StepOver:
                    SignalStepOver(thread, nextAction);
                    break;
                case StepType.StepOut:
                    SignalStepOut(thread, nextAction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stepType), stepType, null);
            }
        }

        private void SignalStepIn(IDebuggeeThread thread, DebuggerAction nextAction)
        {
            // Set trap flag to signal an EXCEPTION_SINGLE_STEP event after next instruction.
            var threadContext = thread.GetThreadContext();
            threadContext.GetRegisterByName("tf").Value = true;
            threadContext.Flush();
            _session.SignalDebuggerLoop(nextAction);
        }

        private void SignalStepOver(IDebuggeeThread thread, DebuggerAction nextAction)
        {
            throw new NotImplementedException();
        }

        private static void SignalStepOut(IDebuggeeThread thread, DebuggerAction nextAction)
        {
            throw new NotSupportedException();
        }

        public void HandleBreakpointEvent(BreakpointEventArgs eventArgs)
        {
            if (eventArgs.Breakpoint is Int3Breakpoint breakpoint)
            {
                breakpoint.HandleBreakpointEvent(eventArgs);
                _breakpointsToRestore.Add(breakpoint);
            }
        }

        public DebuggerAction HandleStepEvent(IDebuggeeThread thread)
        {
            return FinalizeStep(thread);
        }
        
        private void RestoreBreakpoints()
        {
            foreach (var bp in _breakpointsToRestore.Where(x => x.Enabled))
                bp.InstallInt3();
            _breakpointsToRestore.Clear();
        }

        private DebuggerAction FinalizeStep(IDebuggeeThread thread)
        {
            RestoreBreakpoints();
            IsStepping = false;

            if (IsContinuing)
                return FinalizeContinue(thread);

            var eventArgs = new DebuggeeThreadEventArgs(thread)
            {
                NextAction = DebuggerAction.Stop
            };
            OnStepCompleted(eventArgs);
            return eventArgs.NextAction;
        }

        private DebuggerAction FinalizeContinue(IDebuggeeThread thread)
        {
            IsContinuing = false;
            
            var eventArgs = new DebuggeeThreadEventArgs(thread)
            {
                NextAction = _continueAction
            };
            
            OnResumedExecution(eventArgs);
            
            return eventArgs.NextAction;
        }

        protected virtual void OnResumedExecution(DebuggeeThreadEventArgs e)
        {
            ResumedExecution?.Invoke(this, e);
        }

        protected virtual void OnStepCompleted(DebuggeeThreadEventArgs e)
        {
            StepCompleted?.Invoke(this, e);
        }
    }
}