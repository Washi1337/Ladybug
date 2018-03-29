﻿namespace Ladybug.Core
{
    public interface IDebuggeeThread
    {
        IDebuggeeProcess Process
        {
            get;
        }
        
        int Id
        {
            get;
        }

        int ExitCode
        {
            get;
        }
    }
}