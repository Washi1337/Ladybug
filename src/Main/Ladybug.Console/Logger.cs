using System;
using System.Collections.Generic;

namespace Ladybug.Console
{
    public class Logger
    {
        public Logger()
        {
            ForegroundColors = new Dictionary<LoggerMessageType, ConsoleColor>()
            {
                [LoggerMessageType.Default] = ConsoleColor.Gray,
                [LoggerMessageType.Log] = ConsoleColor.DarkGray,
                [LoggerMessageType.Breakpoint] = ConsoleColor.Cyan,
                [LoggerMessageType.OutputString] = ConsoleColor.White,
                [LoggerMessageType.Warning] = ConsoleColor.Yellow,
                [LoggerMessageType.Error] = ConsoleColor.Red,
            };
        }
        
        public IDictionary<LoggerMessageType, ConsoleColor> ForegroundColors
        {
            get;
        } 
        
        public void Write(string message)
        {
            Write(LoggerMessageType.Log, message);
        }
        
        public void Write(string message, params object[] arguments)
        {
            Write(LoggerMessageType.Log, message, arguments);
        }

        public void Write(LoggerMessageType messageType, string message, params object[] arguments)
        {
            System.Console.ForegroundColor = ForegroundColors[messageType];
            System.Console.Write(message, arguments);
            System.Console.ForegroundColor = ForegroundColors[LoggerMessageType.Default];
        }
        
        public void WriteLine(string message)
        {
            WriteLine(LoggerMessageType.Log, message);
        }
        
        public void WriteLine(string message, params object[] arguments)
        {
            WriteLine(LoggerMessageType.Log, message, arguments);
        }

        public void WriteLine(LoggerMessageType messageType, string message, params object[] arguments)
        {
            System.Console.ForegroundColor = ForegroundColors[messageType];
            if (arguments.Length == 0)
                System.Console.WriteLine(message);
            else
                System.Console.WriteLine(message, arguments);
            System.Console.ForegroundColor = ForegroundColors[LoggerMessageType.Default];
        }
    }
}