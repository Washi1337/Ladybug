using System.Collections.Generic;

namespace Ladybug.Core
{
    /// <summary>
    /// Represents a snapshot of a context of a thread that is running in a debuggee process.
    /// </summary>
    public interface IThreadContext
    {
        /// <summary>
        /// Gets a collection of all top level registers and their values.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IRegister> GetTopLevelRegisters();

        /// <summary>
        /// Gets a collection of all registers and their values. This includes all nested registers as well. 
        /// </summary>
        /// <returns></returns>
        IEnumerable<IRegister> GetAllRegisters();
        
        /// <summary>
        /// Gets a register by its name. 
        /// </summary>
        /// <param name="name">The name of the register.</param>
        /// <returns>The register</returns>
        IRegister GetRegisterByName(string name);
        
        /// <summary>
        /// Commits all changes of the context, updating the state of the running thread.  
        /// </summary>
        void Flush();
    }
}