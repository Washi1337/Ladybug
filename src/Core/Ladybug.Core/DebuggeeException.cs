namespace Ladybug.Core
{
    public class DebuggeeException
    {
        public DebuggeeException(uint errorCode, string message, bool isFirstChance, bool continuable)
        {
            ErrorCode = errorCode;
            Message = message;
            IsFirstChance = isFirstChance;
            Continuable = continuable;
        }
        
        public uint ErrorCode
        {
            get;
        }

        public string Message
        {
            get;
        }

        public bool IsFirstChance
        {
            get;
        }

        public bool Continuable
        {
            get;
        }
    }
}