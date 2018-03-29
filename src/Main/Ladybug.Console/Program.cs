using System;
using System.Diagnostics;
using Ladybug.Core;
using Ladybug.Core.Windows;

namespace Ladybug.Console
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: Ladybug.Console.exe <file> [arguments]");
                return;
            }
            
            var session = new DebuggerSession();
            session.ProcessStarted += SessionOnProcessStarted;
            session.ProcessTerminated += SessionOnProcessTerminated;
            session.ThreadStarted += SessionOnThreadStarted;
            session.ThreadTerminated += SessionOnThreadTerminated;
            session.OutputStringSent += SessionOnOutputStringSent;
            session.Paused += SessionOnPaused;
            
            session.StartProcess(new DebuggerProcessStartInfo
            {
                CommandLine = string.Join(" ", args) 
            });
            

            Process.GetCurrentProcess().WaitForExit();
        }

        private static void SessionOnOutputStringSent(object sender, DebuggeeOutputStringEventArgs args)
        {
            System.Console.WriteLine(args.Message);
        }

        private static void SessionOnPaused(object sender, DebuggeeThreadEventArgs args)
        {
            System.Console.WriteLine("Press a key to continue...");
            System.Console.ReadKey();
            args.NextAction = DebuggerAction.Continue;
        }

        private static void SessionOnProcessStarted(object sender, DebuggeeProcessEventArgs args)
        {
            System.Console.WriteLine("Process created. ID: " + args.Process.Id);
        }

        private static void SessionOnProcessTerminated(object sender, DebuggeeProcessEventArgs args)
        {
            System.Console.WriteLine("Process terminated. ID: " + args.Process.Id);
        }

        private static void SessionOnThreadStarted(object sender, DebuggeeThreadEventArgs args)
        {
            System.Console.WriteLine("Thread created. ID: " + args.Thread.Id);
        }

        private static void SessionOnThreadTerminated(object sender, DebuggeeThreadEventArgs args)
        {
            System.Console.WriteLine("Thread terminated. ID: " + args.Thread.Id);
        }
    }
}