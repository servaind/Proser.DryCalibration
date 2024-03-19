using Proser.DryCalibration.util;
using System;
using System.Diagnostics;
using System.IO;

namespace Proser.DryCalibration.log
{
    public static class Log
    {
        private static string logFile = "Proser.DryCalibration.log";

        public static bool WriteIfExists(string message, string fileName)
        {
            return Write(message, fileName, true);
        }

        public static bool Write(string message, string fileName)
        {
            return Write(message, fileName, false);
        }

        public static bool WriteIfExists(string message)
        {
            return Write(message, logFile, true);
        }

        public static bool WriteIfExists(string message, Exception e)
        {
            message = string.Format("{0} Detalle: {1}", message, e.Message);

            return Write(message, logFile, true);
        }

        public static bool WriteIfExists(string message, string logFile, Exception e)
        {
            message = string.Format("{0} Detalle: {1}", message, e.Message);

            return Write(message, logFile, true);
        }

        public static bool Write(string message)
        {
            return Write(message, logFile, false);
        }

        private static bool Write(string message, string fileName, bool checkExists)
        {
            bool result;

            string path = Path.Combine(Utils.GetCurrentPath(), fileName);

            if (checkExists && !File.Exists(path)) return false;

            try
            {
                var sw = new StreamWriter(path, true);
                var s = string.Format("{0} - {1}", DateTime.Now.ToString(), message);
                sw.WriteLine(s);
                sw.Close();

                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        public static void ToConsole(string message)
        {
            ToConsole(message, ConsoleColor.White);
        }

        public static void ToConsole(string message, ConsoleColor color)
        {
            var s = string.Format("{0} - {1}", DateTime.Now.ToString(), message);

            Console.ForegroundColor = color;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void ToEventLog(string source, string message, EventLogEntryType type)
        {
            try
            {
                var log = new EventLog("");
                log.Source = source;
                log.WriteEntry(message, type);
            }
            catch
            {
                
            }
        }
    }
}
