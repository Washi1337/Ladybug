using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IDictionary<IDebuggeeProcess, DisassemblerInfo> _disassemblers = new Dictionary<IDebuggeeProcess, DisassemblerInfo>();
        private readonly ISet<Int3Breakpoint> _breakpointsToRestore = new HashSet<Int3Breakpoint>();
        private readonly ISet<PageGuard> _pageGuardsToRestore = new HashSet<PageGuard>();
            
        
        private bool _isStepping;
        private bool _isContinuing;
        private bool _isRestoringFromGuard;
        
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

        private bool HasBreakpointsToRestore
        {
            get { return _breakpointsToRestore.Count > 0 || _pageGuardsToRestore.Count > 0; }
        }

        public void Continue(DebuggeeThread thread, DebuggerAction nextAction)
        {
            _isContinuing = true;
            _continueAction = nextAction;

            if (HasBreakpointsToRestore)
                SignalStep(thread, StepType.StepIn, nextAction);
            else
                _session.SignalDebuggerLoop(FinalizeContinue(thread));
        }

        public void SignalStep(DebuggeeThread thread, StepType stepType, DebuggerAction nextAction)
        {
            _isStepping = true;
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

            PrepareContextForSingleStep(thread);
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

        private static void PrepareContextForSingleStep(DebuggeeThread thread)
        {
            var threadContext = thread.GetThreadContext();
            threadContext.GetRegisterByName("tf").Value = true;
            threadContext.Flush();
        }

        public bool HandleBreakpointEvent(DEBUG_EVENT debugEvent, out DebuggerAction nextAction)
        {
            // If signalled by an int3, the exception was thrown after the execution of int3.
            // Find corresponding breakpoint and restore the instruction pointer so that it seems
            // it has paused execution before the int3.

            var process = _session.GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);
            
            uint eip = (uint) thread.GetThreadContext().GetRegisterByName("eip").Value - 1;
            var breakpoint = process.GetSoftwareBreakpointByAddress((IntPtr) eip);

            // Check if breakpoint originated from a step-over action.
            if (breakpoint == null && _stepOverBreakpoint?.Address == (IntPtr) eip)
            {
                _stepOverBreakpoint.HandleBreakpointEvent(new BreakpointEventArgs(thread, _stepOverBreakpoint));
                _stepOverBreakpoint = null;
                nextAction = FinalizeStep(thread);
                return true;
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
                nextAction = eventArgs.NextAction;
                return true;
            }

            nextAction = DebuggerAction.ContinueWithException;
            return false;
        }

        public bool HandleStepEvent(DEBUG_EVENT debugEvent, out DebuggerAction nextAction)
        {
            var thread = _session.GetProcessById((int) debugEvent.dwProcessId)
                .GetThreadById((int) debugEvent.dwThreadId);
            
            nextAction = FinalizeStep(thread);
            return true;
        }

        public bool HandlePageGuardViolationEvent(DEBUG_EVENT debugEvent, out DebuggerAction nextAction)
        {
            // Memory breakpoints are implemented using page guards. We need to check if the page guard
            // violation originated from a breakpoint or not, and pause or continue execution when appropriate.
            
            const int ExceptionInformationReadWrite = 0;
            const int ExceptionInformationAddress = 1;
        
            var info = debugEvent.InterpretDebugInfoAs<EXCEPTION_DEBUG_INFO>();
            var process = _session.GetProcessById((int) debugEvent.dwProcessId);
            var thread = process.GetThreadById((int) debugEvent.dwThreadId);

            if (info.ExceptionRecord.NumberParameters >= 2)
            {
                bool isWrite = info.ExceptionRecord.ExceptionInformation[ExceptionInformationReadWrite] == 1;
                IntPtr address = (IntPtr) info.ExceptionRecord.ExceptionInformation[ExceptionInformationAddress];

                var pageGuard = process.GetContainingPageGuard(address);
                if (pageGuard != null)
                {
                    // Restore page guard after continuing.
                    _pageGuardsToRestore.Add(pageGuard);

                    if (pageGuard.Breakpoints.TryGetValue(address, out var breakpoint)
                        && (breakpoint.BreakOnRead == !isWrite || breakpoint.BreakOnWrite == isWrite))
                    {
                        // Violation originated from a breakpoint.
                        var eventArgs = new BreakpointEventArgs(thread, breakpoint)
                        {
                            NextAction = DebuggerAction.Stop
                        };

                        breakpoint.HandleBreakpointEvent(eventArgs);
                        OnBreakpointHit(eventArgs);
                        nextAction = eventArgs.NextAction;
                    }
                    else
                    {
                        // Violation did not originate from a breakpoint.
                        _isRestoringFromGuard = true;
                        PrepareContextForSingleStep(thread);
                        nextAction = DebuggerAction.Continue;
                    }

                    return true;
                }
            }
            

            nextAction = DebuggerAction.ContinueWithException;            
            return false;
        }

        private void RestoreBreakpoints()
        {
            foreach (var bp in _breakpointsToRestore.Where(x => x.Enabled))
                bp.InstallInt3();
            _breakpointsToRestore.Clear();

            foreach (var pg in _pageGuardsToRestore.Where(x => x.Enabled))
                pg.InstallPageGuard();
            _pageGuardsToRestore.Clear();
        }

        private DebuggerAction FinalizeStep(DebuggeeThread thread)
        {
            RestoreBreakpoints();
            
            _isStepping = false;

            if (_isRestoringFromGuard)
                return FinalizeRestoreFromGuard(thread);
            
            if (_isContinuing)
                return FinalizeContinue(thread);

            var eventArgs = new DebuggeeThreadEventArgs(thread)
            {
                NextAction = DebuggerAction.Stop
            };
            OnStepCompleted(eventArgs);
            return eventArgs.NextAction;
        }

        private DebuggerAction FinalizeRestoreFromGuard(DebuggeeThread thread)
        {
            _isRestoringFromGuard = false;
            return DebuggerAction.Continue;
        }

        private DebuggerAction FinalizeContinue(IDebuggeeThread thread)
        {
            _isContinuing = false;
            
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