namespace Ladybug.Core
{
    /// <summary>
    /// Represents an exception that occurred in a debuggee process.
    /// </summary>
    public class DebuggeeException
    {
        public DebuggeeException(uint errorCode, string message, bool isFirstChance, bool continuable)
        {
            ErrorCode = errorCode;
            Message = message;
            IsFirstChance = isFirstChance;
            Continuable = continuable;
        }
        
        /// <summary>
        /// Gets the raw error code of the exception that was thrown.
        /// </summary>
        public uint ErrorCode
        {
            get;
        }

        /// <summary>
        /// Gets a descriptive message of the exception that was thrown.
        /// </summary>
        public string Message
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the exception was a first-chance exception or not.
        /// </summary>
        public bool IsFirstChance
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the exception is continuable or not.
        /// </summary>
        public bool Continuable
        {
            get;
        }
    }
}