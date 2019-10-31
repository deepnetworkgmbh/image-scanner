using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace kube_scanner.helpers
{
    public static class LogHelper
    {
        private static string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss:ffff");   
        }

        public static void LogErrorsAndExit(params object[] messages)
        {
            LogErrors(messages); // write logs
            
            Environment.Exit(1); // exit and return code 1
        }
        
        public static void LogErrorsAndContinue(params object[] messages)
        {
            LogErrors(messages); // write logs
        }

        private static void LogErrors(params object[] messages)
        {
            // aggregate log messages into a string
            var logMsg = messages.Aggregate(string.Empty, (current, message) => current + message + " : ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("{0} ERROR : {1}", GetTimeStamp(), logMsg);
            Console.ResetColor();
        }
        
        public static void LogMessages(params object[] messages)
        {
            // aggregate log messages into a string
            var logMsg = messages.Aggregate(string.Empty, (current, message) => current + message + " ");

            Console.WriteLine("{0} {1}", GetTimeStamp(), logMsg);
        }
        
        public static void LogErrors(IEnumerable<Error> errors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Program wasn't able to parse your input");
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }

            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}