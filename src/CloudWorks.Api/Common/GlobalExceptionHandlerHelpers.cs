namespace Api.Common;

internal static partial class GlobalExceptionHandlerHelpers
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    internal static partial void LogUnhandledException(ILogger logger, Exception exception);
}
