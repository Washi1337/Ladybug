using System;

namespace Ladybug.Core
{
    public interface IDebuggeeLibrary
    {
        IDebuggeeProcess Process
        {
            get;
        }
        
        string Name
        {
            get;
        }

        IntPtr BaseOfLibrary
        {
            get;
        }
    }
}