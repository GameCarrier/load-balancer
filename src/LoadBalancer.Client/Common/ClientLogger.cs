using System;

namespace LoadBalancer.Client.Common
{
    class ClientLogger : ConsoleLogger
    {
#if USE_GC_LOGGING
        GameCarrier.Clients.Logger gcLogger;
#endif

        public ClientLogger(string name) : base(name)
        {
#if USE_GC_LOGGING
            gcLogger = new GameCarrier.Clients.Logger(name);
#endif
        }

        public override void Log(LogLevel level, Exception exception, string message, params object[] args)
        {
#if USE_GC_LOGGING
            GameCarrier.Common.LogLevel gcLogLevel = level switch
            {
                LogLevel.Trace => GameCarrier.Common.LogLevel.LLL_DEBUG,
                LogLevel.Debug => GameCarrier.Common.LogLevel.LLL_DEBUG,
                LogLevel.Information => GameCarrier.Common.LogLevel.LLL_NOTICE,
                LogLevel.Warning => GameCarrier.Common.LogLevel.LLL_WARN,
                LogLevel.Error => GameCarrier.Common.LogLevel.LLL_ERR,
                LogLevel.Critical => GameCarrier.Common.LogLevel.LLL_ERR,
                _ => 0,
            };
            gcLogger.LogMessage(gcLogLevel, string.Format(message, args), exception);
#else
            base.Log(level, exception, message, args);
#endif
        }
    }
}
