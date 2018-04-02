using System;
using System.Collections.Generic;
using Ladybug.Core.Windows.Kernel32;

namespace Ladybug.Core.Windows
{
    public class X86ThreadContext32 : IThreadContext
    {
        private readonly IDictionary<string, IRegister> _registers = new Dictionary<string, IRegister>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, IRegister> _allRegisters = new Dictionary<string, IRegister>(StringComparer.OrdinalIgnoreCase);
        
        private readonly IntPtr _threadHandle;
        private CONTEXT _context;
        
        internal X86ThreadContext32(IntPtr threadHandle)
        {
            _threadHandle = threadHandle;
            
            _context = NativeMethods.GetThreadContext32(threadHandle);
            
            _registers["eax"] = new Register<uint>("eax", _context.Eax);
            _registers["ebx"] = new Register<uint>("ebx", _context.Ebx);
            _registers["ecx"] = new Register<uint>("ecx", _context.Ecx);
            _registers["edx"] = new Register<uint>("edx", _context.Edx);
            _registers["esp"] = new Register<uint>("esp", _context.Esp);
            _registers["ebp"] = new Register<uint>("ebp", _context.Ebp);
            _registers["esi"] = new Register<uint>("esi", _context.Esi);
            _registers["edi"] = new Register<uint>("edi", _context.Edi);
            _registers["eip"] = new Register<uint>("eip", _context.Eip);
            _registers["eflags"] = new FlagsRegister32(_context.EFlags);
            
            var stack = new Stack<IRegister>();
            foreach (var register in _registers.Values)
                stack.Push(register);
            
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                _allRegisters[current.Name] = current;
                
                if (current is ICompoundedRegister compoundedRegister)
                {
                    foreach (var child in compoundedRegister.Parts)
                        stack.Push(child);
                }
                    
            }
        }

        public IEnumerable<IRegister> GetTopLevelRegisters()
        {
            return _registers.Values;
        }

        public IEnumerable<IRegister> GetAllRegisters()
        {
            return _allRegisters.Values;
        }

        public IRegister GetRegisterByName(string name)
        {
            return _allRegisters[name];
        }

        public void Flush()
        {
            _context.Eax = (uint) _registers["eax"].Value;
            _context.Ebx = (uint) _registers["ebx"].Value;
            _context.Ecx = (uint) _registers["ecx"].Value;
            _context.Edx = (uint) _registers["edx"].Value;
            _context.Esp = (uint) _registers["esp"].Value;
            _context.Ebp = (uint) _registers["ebp"].Value;
            _context.Esi = (uint) _registers["esi"].Value;
            _context.Edi = (uint) _registers["edi"].Value;

            _context.Eip = (uint) _registers["eip"].Value;
            _context.EFlags = (uint) _registers["eflags"].Value;
            
            NativeMethods.SetThreadContext32(_threadHandle, _context);
        }
    }
}