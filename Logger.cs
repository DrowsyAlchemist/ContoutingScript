using System;

namespace ContouringScript
{
    static class Logger
    {
        private const ConsoleColor InfoColor = ConsoleColor.Green;
        private const ConsoleColor WarningColor = ConsoleColor.Yellow;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;

        public static bool IsDebugMode { get; set; } = false;

        public static void WriteInfo(string message)
        {
            if (IsDebugMode)
                WriteMessage(message, InfoColor);
        }

        public static void WriteWarning(string message)
        {
            if (IsDebugMode)
                WriteMessage(message, WarningColor);
        }

        public static void WriteError(string message)
        {
            WriteMessage(message, ErrorColor);
        }

        private static void WriteMessage(string message, ConsoleColor color)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = defaultColor;
        }
    }
}