namespace Ladybug.Core
{
    public class DebuggeeLibraryEventArgs : DebuggeeThreadEventArgs
    {
        public DebuggeeLibraryEventArgs(IDebuggeeThread thread, IDebuggeeLibrary library)
            : base(thread)
        {
            Library = library;
        }
        
        public IDebuggeeLibrary Library
        {
            get;
        }
    }
}