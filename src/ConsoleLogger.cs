using System;

namespace QuickFileManager
{
    public class ConsoleLogger : ILogger
    {
        public void Info(string msg)
        {
            Console.WriteLine($"[INFO] {msg}");
        }

        public void Warn(string msg)
        {
            Console.WriteLine($"[WARN] {msg}");
        }

        public void Error(string msg)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {msg}");
            Console.ForegroundColor = prev;
        }
    }
}