namespace LoadBalancer.Server.Common
{
    class ServerLogger : ConsoleLogger
    {
#if USE_GC_LOGGING
        static ServerLogger()
        {
            GameCarrier.Common.LogLevel gcLogLevel = (GameCarrier.Common.LogLevel)GameCarrier.Adapter.GCConfig.Read("logging.level");
            Default = gcLogLevel switch
            {
                GameCarrier.Common.LogLevel.LLL_VERBOSE => LogLevel.Trace,
                GameCarrier.Common.LogLevel.LLL_DEBUG => LogLevel.Debug,
                GameCarrier.Common.LogLevel.LLL_INFO => LogLevel.Information,
                GameCarrier.Common.LogLevel.LLL_NOTICE => LogLevel.Information,
                GameCarrier.Common.LogLevel.LLL_NORMAL => LogLevel.Information,
                GameCarrier.Common.LogLevel.LLL_WARN => LogLevel.Warning,
                GameCarrier.Common.LogLevel.LLL_ERR => LogLevel.Error,
                _ => LogLevel.None,
            };
        }
#endif

#if USE_GC_LOGGING
        GameCarrier.Adapter.Logger gcLogger;
#endif

        public ServerLogger(string name) : base(name)
        {
#if USE_GC_LOGGING
            gcLogger = new GameCarrier.Adapter.Logger(name);
#endif
        }

        public override void Log(LogLevel level, Exception exception, string message, params object[] args)
        {
            if (level < Default) return;

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
