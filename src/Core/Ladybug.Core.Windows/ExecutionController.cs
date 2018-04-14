using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.X86;
using Ladybug.Core.Windows.Kernel32.Debugging;

namespace Ladybug.Core.Windows
{
    internal class ExecutionController
    {
        private struct DisassemblerInfo
        {
            public readonly ProcessMemoryReader Reader;
            public readonly X86Disassembler Disassembler;

            public DisassemblerInfo(ProcessMemoryReader reader)
            {
                Reader = reader;
                Disassembler = new X86Disassembler(reader);
            }
        }
        
        public event EventHandler<DebuggeeThreadEventArgs> ResumedExecution;
        public event EventHandler<DebuggeeThreadEventArgs> StepCompleted;
        public event EventHandler<BreakpointEventArgs> BreakpointHit; 
        
        private readonly DebuggerSession _session;
        private readonly IList<Int3Breakpoint> _breakpointsToRestore = new List<Int3Breakpoint>();
        private readonly IDictionary<IDebuggeeProcess, DisassemblerInfo> _disassemblers = new Dictionary<IDebuggeeProcess, DisassemblerInfo>();
        private DebuggerAction _continueAction;
        private Int3Breakpoint _stepOverBreakpoint;

        public ExecutionController(DebuggerSession session)
        {
            _session = session;
            
            session.ProcessStarted += (sender, args) =>
                _disassemblers.Add(args.Process, new DisassemblerInfo(new ProcessMemoryReader(args.Process)));
                
            session.ProcessTerminated += (sender, args) =>
                _disassemblers.Remove(args.Process);
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

        private void SignalStepIn(DebuggeeThread thread, DebuggerAction nextAction)
        {
            // Set trap flag to signal an EXCEPTION_SINGLE_STEP event after next instruction.
            
            var threadContext = thread.GetThreadContext();
            threadContext.GetRegisterByName("tf").Value = true;
            threadContext.Flush();
            _session.SignalDebuggerLoop(nextAction);
        }

        private void SignalStepOver(DebuggeeThread thread, DebuggerAction nextAction)
        {
            // Stepping over means step one instruction, but skip call instructions.
            // Therefore, if the current instruction is a call, we set a temporary breakpoint
            // to the next instruction and continue execution, otherwise we perform a normal step.
            
            var threadContext = thread.GetThreadContext();
            var eip = (uint) threadContext.GetRegisterByName("eip").Value;
            
            var info = _disassemblers[thread.Process];
            info.Reader.Position = eip;
            var instruction = info.Disassembler.ReadNextInstruction();
            
            switch (instruction.Mnemonic)
            {
                case X86Mnemonic.Call:
                case X86Mnemonic.Call_Far:
                    _stepOverBreakpoint = new Int3Breakpoint(thread.Process, (IntPtr) info.Reader.Position, true);
                    _session.SignalDebuggerLoop(nextAction);
                    break;
                default:
                    SignalStepIn(thread, nextAction);
                    break;
            }
        }

        private static void SignalStepOut(IDebuggeeThread thread, DebuggerAction nextAction)
        {
            throw new NotSupportedException();
        }

        public DebuggerAction HandleBreakpointEvent(DEBUG_EVENT debugEvent)
        {
            // If signalled by an int3, the exception was thrown after the execution of int3.
            // Find corresponding breakpoint and restore the instruction pointer so that it seems
            // it has paused execution before the int3.

            var process = _session.GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);
            
            uint eip = (uint) thread.GetThreadContext().GetRegisterByName("eip").Value - 1;
            var breakpoint = process.GetBreakpointByAddress((IntPtr) eip);

            // Check if breakpoint originated from a step-over action.
            if (breakpoint == null && _stepOverBreakpoint?.Address == (IntPtr) eip)
            {
                _stepOverBreakpoint.HandleBreakpointEvent(new BreakpointEventArgs(thread, _stepOverBreakpoint));
                _stepOverBreakpoint = null;
                return FinalizeStep(thread);
            }
            
            if (breakpoint != null)
            {
                _breakpointsToRestore.Add(breakpoint);
                var eventArgs = new BreakpointEventArgs(thread, breakpoint)
                {
                    NextAction = DebuggerAction.Stop
                };
                
                breakpoint.HandleBreakpointEvent(eventArgs);
                OnBreakpointHit(eventArgs);
                return eventArgs.NextAction;
            }

            return DebuggerAction.Stop;
        }

        public DebuggerAction HandleStepEvent(DEBUG_EVENT debugEvent)
        {
            var thread = _session.GetProcessById((int) debugEvent.dwProcessId)
                .GetThreadById((int) debugEvent.dwThreadId);
            
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

        protected virtual void OnBreakpointHit(BreakpointEventArgs e)
        {
            BreakpointHit?.Invoke(this, e);
        }
    }
}