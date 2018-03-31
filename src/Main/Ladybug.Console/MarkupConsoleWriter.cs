using System;
using System.IO;
using System.Text;

namespace Ladybug.Console
{
    internal class MarkupConsoleWriter : TextWriter
    {
        private readonly TextWriter _originalOut;

        public MarkupConsoleWriter(TextWriter originalOut)
        {
            _originalOut = originalOut;
            ForegroundColor = ConsoleColor.DarkGray;
        }

        public ConsoleColor ForegroundColor
        {
            get;
            set;
        }
        
        public override Encoding Encoding
        {
            get { return _originalOut.Encoding; }
        }
        
        public override void WriteLine(string value)
        {
            System.Console.ForegroundColor = ForegroundColor;
            _originalOut.WriteLine(value);
            System.Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}