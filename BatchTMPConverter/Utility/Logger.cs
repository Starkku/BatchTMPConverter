/*
 * Copyright 2016-2022 by Starkku
 * This file is part of BatchTMPConverter, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace BatchTMPConverter.Utility
{
    public static class Logger
    {
        private static readonly ConsoleColor DEFAULT_CONSOLE_COLOR = Console.ForegroundColor;
        private static StreamWriter LOG_WRITER = null;
        private static Stopwatch TIMER = null;

        public static void EnableWriteToFile(string logFilename = null)
        {
            string filename = logFilename;
            if (string.IsNullOrEmpty(filename))
                filename = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".log";

            File.Delete(filename);
            LOG_WRITER = File.CreateText(filename);
            LOG_WRITER.AutoFlush = true;

            TIMER = new Stopwatch();
            TIMER.Start();
        }

        public static void Info(string str)
        {
            Console.ForegroundColor = DEFAULT_CONSOLE_COLOR;
            Console.WriteLine(str);
                LogToFile("Info", str);
        }

        public static void Warn(string str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(str);
            Console.ForegroundColor = DEFAULT_CONSOLE_COLOR;
                LogToFile("Warn", str);
        }

        public static void Error(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ForegroundColor = DEFAULT_CONSOLE_COLOR;
                LogToFile("Error", str);
        }

        private static void LogToFile(string label, string str)
        {
            if (LOG_WRITER == null)
                return;

            LOG_WRITER.WriteLine(GetTime() + (string.IsNullOrEmpty(label) ? "" : " [" + label + "]") + " " + str);
        }

        private static string GetTime()
        {
            string dateString = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            return dateString + " | " + TIMER.Elapsed.ToString();
        }
    }
}
