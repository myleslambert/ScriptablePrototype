using System.Linq;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using BedrockFramework.Utilities;

namespace BedrockFramework.Logger
{
    public static class Logger
    {
        public struct LoggerLog
        {
            public string logCategory;
            public string logMessage;
            public LogType logType;
            public string logStackTrace;

            public LoggerLog (string category, string message, LogType type, string stackTrace)
            {
                logCategory = category;
                logMessage = message;
                logType = type;
                logStackTrace = stackTrace;
            }
        }

        public static readonly string defaultCategory = "Default";
        public static List<LoggerLog> currentLogs = new List<LoggerLog>();
        public delegate void LogAdded(LoggerLog newLog);
        public static event LogAdded OnLogAdded;

        static readonly string formatString = "{}";
        static readonly int formatStringLength = 2;

        static StringBuilder builder = new StringBuilder();

        /// 
        /// Logs
        /// 

        public static void Log(object logObject)
        {
            _Log(LogType.Log, defaultCategory, logObject.ToString(), new object[0]);
        }

        public static void Log(string category, object logObject)
        {
            _Log(LogType.Log, category, logObject.ToString(), new object[0]);
        }

        public static void Log(string baseString, params object[] list)
        {
            _Log(LogType.Log, defaultCategory, baseString, list);
        }

        public static void Log(string category, string baseString, params object[] list)
        {
            _Log(LogType.Log, category, baseString, list);
        }

        /// 
        /// Warnings
        /// 

        public static void LogWarning(object logObject)
        {
            _Log(LogType.Warning, defaultCategory, logObject.ToString(), new object[0]);
        }

        public static void LogWarning(string category, object logObject)
        {
            _Log(LogType.Warning, category, logObject.ToString(), new object[0]);
        }

        public static void LogWarning(string baseString, params object[] list)
        {
            _Log(LogType.Warning, defaultCategory, baseString, list);
        }

        public static void LogWarning(string category, string baseString, params object[] list)
        {
            _Log(LogType.Warning, category, baseString, list);
        }

        /// 
        /// Errors
        /// 

        public static void LogError(object logObject)
        {
            _Log(LogType.Error, defaultCategory, logObject.ToString(), new object[0]);
        }

        public static void LogError(string category, object logObject)
        {
            _Log(LogType.Error, category, logObject.ToString(), new object[0]);
        }

        public static void LogError(string baseString, params object[] list)
        {
            _Log(LogType.Error, defaultCategory, baseString, list);
        }

        public static void LogError(string category, string baseString, params object[] list)
        {
            _Log(LogType.Error, category, baseString, list);
        }

        /// 
        /// Generics
        /// 

        private static void _Log(LogType logType, string category, string baseString, params object[] list)
        {
            builder.Length = 0;
            builder.Append(baseString);

            for (int i = 0; i < list.Length; i++)
            {
                int formatPos = builder.IndexOf(formatString, 0, false);
                if (formatPos < 0)
                    break;

                builder.Replace(formatString, list[i].ToString(), formatPos, formatStringLength);
            }

            AddLog(logType, category, builder.ToString(), UnityEngine.StackTraceUtility.ExtractStackTrace());
        }

        static void AddLog(LogType logType, string category, string log, string stackTrace)
        {
            LoggerLog newLog = new LoggerLog(category, log, logType, stackTrace);
            currentLogs.Add(newLog);

            if (OnLogAdded != null)
                OnLogAdded(newLog);
        }


    } 
}