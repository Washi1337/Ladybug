namespace Ladybug.Console.X64
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            new ConsoleDebugger(args).Run();
        }
    }
}