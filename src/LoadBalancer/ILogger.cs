using System;
using System.Threading;

namespace LoadBalancer
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6,
    }

    public interface ILogger
    {
        bool IsTraceEnabled { get; }
        bool IsDebugEnabled { get; }
        bool IsInformationEnabled { get; }
        bool IsWarningEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsCriticalEnabled { get; }

        void Log(LogLevel level, string message, params object[] args);
        void Log(LogLevel level, Exception exception, string message, params object[] args);
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogCritical(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogCritical(Exception exception, string message, params object[] args);
    }

    public class ConsoleLogger : ILogger
    {
        public static LogLevel Default = LogLevel.Information;

        public string Name { get; private set; }

        public ConsoleLogger(string name)
        {
            Name = name;
        }

        public virtual void Log(LogLevel level, string message, params object[] args) =>
            Log(level, null, message, args);

        public virtual void Log(LogLevel level, Exception exception, string message, params object[] args)
        {
            if (level < Default) return;

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff}] ({Thread.CurrentThread.ManagedThreadId}) {level.ToString().ToUpper()} [{Name}]: {string.Format(message, args)}");

            if (exception != null)
                LogException(exception);
        }

        private void LogException(Exception exception)
        {
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);

            if (exception is AggregateException agg && agg.InnerExceptions != null)
            {
                foreach (var inner in agg.InnerExceptions)
                    if (inner != null)
                        LogException(inner);
            }
            else
            {
                var inner = exception.InnerException;
                if (inner != null)
                    LogException(inner);
            }
        }

        public bool IsTraceEnabled => Default <= LogLevel.Trace;
        public bool IsDebugEnabled => Default <= LogLevel.Debug;
        public bool IsInformationEnabled => Default <= LogLevel.Information;
        public bool IsWarningEnabled => Default <= LogLevel.Warning;
        public bool IsErrorEnabled => Default <= LogLevel.Error;
        public bool IsCriticalEnabled => Default <= LogLevel.Critical;

        public void LogTrace(string message, params object[] args) =>
            Log(LogLevel.Trace, message, args);

        public void LogDebug(string message, params object[] args) =>
            Log(LogLevel.Debug, message, args);

        public void LogInformation(string message, params object[] args) =>
            Log(LogLevel.Information, message, args);

        public void LogWarning(string message, params object[] args) =>
            Log(LogLevel.Warning, message, args);

        public void LogError(string message, params object[] args) =>
            Log(LogLevel.Error, message, args);

        public void LogCritical(string message, params object[] args) =>
            Log(LogLevel.Critical, message, args);

        public void LogError(Exception exception, string message, params object[] args) =>
            Log(LogLevel.Error, exception, message, args);

        public void LogCritical(Exception exception, string message, params object[] args) =>
            Log(LogLevel.Critical, exception, message, args);
    }
}
