using System;
using System.Collections.Generic;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    internal class PageGuard
    {
        private readonly IntPtr _processHandle;
        private MemoryProtection _oldProtection;
        private bool _enabled;

        public PageGuard(IntPtr processHandle, IntPtr address)
        {
            _processHandle = processHandle;
            Address = address;
            Breakpoints = new Dictionary<IntPtr, PageGuardBreakpoint>();
        }

        public IntPtr Address
        {
            get;
        }

        public IDictionary<IntPtr, PageGuardBreakpoint> Breakpoints
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
                    if (value)
                        InstallPageGuard();
                    else
                        RestorePagePermissions();
                } 
            }
        }

        public void InstallPageGuard()
        {
            NativeMethods.VirtualProtectEx(
                _processHandle, Address,
                (UIntPtr) Environment.SystemPageSize,
                MemoryProtection.PAGE_GUARD | MemoryProtection.PAGE_EXECUTE_READWRITE, out var oldProtection);
                
            if (!_enabled)
            {
                _oldProtection = oldProtection;
                _enabled = true;
            }
        }

        public void RestorePagePermissions()
        {
            NativeMethods.VirtualProtectEx(
                _processHandle, Address,
                (UIntPtr) Environment.SystemPageSize,
                _oldProtection, out _oldProtection);
            _enabled = false;
        }
    }
}