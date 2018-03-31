using System.Collections.Generic;

namespace Ladybug.Core
{
    public interface IThreadContext
    {
        IEnumerable<IRegister> GetTopLevelRegisters();

        IEnumerable<IRegister> GetAllRegisters();
        
        IRegister GetRegisterByName(string name);
        
        void Flush();
    }
}