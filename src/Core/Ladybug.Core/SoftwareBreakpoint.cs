using System;

namespace Ladybug.Core
{
    public class SoftwareBreakpoint : IBreakpoint
    {
        public event EventHandler<BreakpointEventArgs> BreakpointHit;

        public bool Enabled
        {
            get;
            set;
        }

        protected virtual void OnBreakpointHit(BreakpointEventArgs e)
        {
            BreakpointHit?.Invoke(this, e);
        }
    }
}