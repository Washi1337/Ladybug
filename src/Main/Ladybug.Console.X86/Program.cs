namespace Ladybug.Console.X86
{
   internal static class Program
   {
      public static void Main(string[] args)
      {
         new ConsoleDebugger(args).Run();
      }
   }
}