using System;

namespace MPQToTACT.Helpers
{
    static class Log
    {
        public static void WriteLine(string message)
        {
            Console.WriteLine($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {message}");
        }
    }
}
