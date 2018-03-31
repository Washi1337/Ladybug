using System;

namespace Ladybug.Core.Windows
{
    public class Int3Breakpoint : IBreakpoint
    {
        public event EventHandler<BreakpointEventArgs> BreakpointHit;
        private readonly DebuggeeProcess _process;
        
        private readonly byte[] _breakpointBytes = {0xCC};
        private byte[] _originalBytes;
        
        private bool _enabled;
        
        public Int3Breakpoint(DebuggeeProcess process, IntPtr address, bool enabled)
        {
            _process = process;
            Address = address;
            Enabled = enabled;
        }
        
        public IntPtr Address
        {
            get;
        }
        
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if (value)
                        InstallInt3();
                    else
                        RestoreOriginalCode();
                }
            }
        }

        public byte[] OriginalBytes
        {
            get { return _originalBytes; }
        }

        public void InstallInt3()
        {
            _originalBytes = new byte[_breakpointBytes.Length];
            _process.ReadMemory(Address, _originalBytes, 0, _originalBytes.Length);
            _process.WriteMemory(Address, _breakpointBytes, 0, _breakpointBytes.Length);
        }

        public void RestoreOriginalCode()
        {
            _process.WriteMemory(Address, _originalBytes, 0, _originalBytes.Length);
        }

        internal void HandleBreakpointEvent(BreakpointEventArgs e)
        {
            var context = e.Thread.GetThreadContext();
            var eip = (Register<uint>) context.GetRegisterByName("eip");
            eip.Value--;
            context.Flush();
            
            RestoreOriginalCode();
            OnBreakpointHit(e);
        }
        
        protected virtual void OnBreakpointHit(BreakpointEventArgs e)
        {
            BreakpointHit?.Invoke(this, e);
        }
    }
}