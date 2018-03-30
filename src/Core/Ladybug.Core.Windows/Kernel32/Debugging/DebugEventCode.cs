namespace Ladybug.Core.Windows.Kernel32.Debugging
{
    internal enum DebugEventCode
    {
        CREATE_PROCESS_DEBUG_EVENT = 3,
        CREATE_THREAD_DEBUG_EVENT = 2,
        EXCEPTION_DEBUG_EVENT = 1,
        EXIT_PROCESS_DEBUG_EVENT = 5,
        EXIT_THREAD_DEBUG_EVENT = 4,
        LOAD_DLL_DEBUG_EVENT = 6,
        OUTPUT_DEBUG_STRING_EVENT = 8,
        RIP_EVENT = 9,
        UNLOAD_DLL_DEBUG_EVENT = 7,
    }
}