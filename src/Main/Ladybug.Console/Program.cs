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
            System.Console.SetOut(new MarkupConsoleWriter(System.Console.Out));
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: Ladybug.Console.exe <file> [arguments]");
                return;
            }
            
            IDebuggerSession session = new DebuggerSession();
            session.ProcessStarted += SessionOnProcessStarted;
            session.ProcessTerminated += SessionOnProcessTerminated;
            session.ThreadStarted += SessionOnThreadStarted;
            session.ThreadTerminated += SessionOnThreadTerminated;
            session.OutputStringSent += SessionOnOutputStringSent;
            session.LibraryLoaded += SessionOnLibraryLoaded;
            session.LibraryUnloaded += SessionOnLibraryUnloaded;
            session.Paused += SessionOnPaused;
            
            var process = session.StartProcess(new DebuggerProcessStartInfo
            {
                CommandLine = string.Join(" ", args) 
            });

            bool exit = false;
            while (!exit)
            {
                string commandLine = System.Console.ReadLine();
                var commandArgs = commandLine.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (commandArgs.Length > 0)
                {
                    switch (commandArgs[0])
                    {
                        case "g":
                        case "go":
                            session.Continue(DebuggerAction.Continue);
                            break;
                        case "break":
                            process.Break();
                            break;
                        case "exit":
                            exit = true;
                            break;
                    }
                }
            }
        }
        
        private static void SessionOnOutputStringSent(object sender, DebuggeeOutputStringEventArgs args)
        {
            System.Console.WriteLine(args.Message);
        }

        private static void SessionOnPaused(object sender, DebuggeeThreadEventArgs args)
        {
            System.Console.WriteLine("Debuggee paused.");
            foreach (var register in args.Thread.ThreadContext.GetTopLevelRegisters())
            {
                ulong value = Convert.ToUInt64(register.Value);
                System.Console.WriteLine("{0}: 0x{1} ({2})", register.Name, value.ToString("X" + register.Size / 4), value);
            }
        }

        private static void SessionOnProcessStarted(object sender, DebuggeeProcessEventArgs args)
        {
            System.Console.WriteLine("Process created. ID: " + args.Process.Id);
            args.NextAction = DebuggerAction.Stop;
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

        private static void SessionOnLibraryLoaded(object sender, DebuggeeLibraryEventArgs args)
        {
            System.Console.WriteLine("Loaded library " + (args.Library.Name ?? "<no name>") + " at "
                                     + args.Library.BaseOfLibrary.ToInt64().ToString("X8"));
        }

        private static void SessionOnLibraryUnloaded(object sender, DebuggeeLibraryEventArgs args)
        {
            System.Console.WriteLine("Unloaded library " + (args.Library.Name ?? "<no name>") + " at "+ args.Library.BaseOfLibrary.ToInt64().ToString("X8"));
        }
    }
}