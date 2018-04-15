using System;

namespace Ladybug.Core.Windows
{
    public class PageGuardBreakpoint : IMemoryBreakpoint
    {
        public event EventHandler<BreakpointEventArgs> BreakpointHit;

        internal PageGuardBreakpoint(PageGuard guard, IntPtr address, bool enabled)
        {
            Guard = guard ?? throw new ArgumentNullException(nameof(guard));
            Address = address;
            Enabled = enabled;
        }

        internal PageGuard Guard
        {
            get;
        }

        public bool Enabled
        {
            get;
            set;
        }

        public IntPtr Address
        {
            get;
        }

        protected virtual void OnBreakpointHit(BreakpointEventArgs e)
        {
            BreakpointHit?.Invoke(this, e);
        }

        public bool BreakOnRead
        {
            get;
            set;
        }

        public bool BreakOnWrite
        {
            get;
            set;
        }

        internal void HandleBreakpointEvent(BreakpointEventArgs e)
        {
            OnBreakpointHit(e);
        }
    }
}