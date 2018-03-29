using System;

namespace Ladybug.Core.Windows
{
    public class DebuggeeLibrary : IDebuggeeLibrary
    {
        public DebuggeeLibrary(IDebuggeeProcess process, string name, IntPtr baseOfLibrary)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            Name = name;
            BaseOfLibrary = baseOfLibrary;
        }
        
        public IDebuggeeProcess Process
        {
            get;
        }

        public string Name
        {
            get;
        }

        public IntPtr BaseOfLibrary
        {
            get;
        }
    }
}