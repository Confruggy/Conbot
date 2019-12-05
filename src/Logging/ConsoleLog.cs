using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace Conbot.Logging
{
    public static class ConsoleLog
    {
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public static LogSeverity Severity = LogSeverity.Verbose;

        public static async Task LogAsync(LogMessage log)
        {
            if (log.Severity > Severity)
                return;

            if (log.Message?.Contains("Received Dispatch") == true)
                return;

            await _lock.WaitAsync().ConfigureAwait(false);
            var now = DateTimeOffset.Now;

            Console.CursorVisible = false;
            await Console.Out.WriteLineAsync();

            Console.ForegroundColor = ConsoleColor.White;
            await Console.Out.WriteAsync(log.Severity.ToString().PadLeft(7)).ConfigureAwait(false);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(" ~");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("> ");
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Out.WriteAsync($"[{log.Source}] ").ConfigureAwait(false);
            Console.ForegroundColor = ConsoleColor.Gray;
            await Console.Out.WriteAsync(log.Message ?? log.Exception.ToString()).ConfigureAwait(false);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            await Console.Out.WriteAsync($" {now:HH\\:mm\\:ss}").ConfigureAwait(false);
            _lock.Release();
        }

        public static Task CriticalAsync(string source, string message, Exception exception = null)
        => LogAsync(new LogMessage(LogSeverity.Critical, source, message, exception));

        public static Task ErrorAsync(string source, string message, Exception exception = null)
        => LogAsync(new LogMessage(LogSeverity.Error, source, message, exception));

        public static Task WarningAsync(string source, string message, Exception exception = null)
        => LogAsync(new LogMessage(LogSeverity.Warning, source, message, exception));

        public static Task InfoAsync(string source, string message, Exception exception = null)
        => LogAsync(new LogMessage(LogSeverity.Info, source, message, exception));

        public static Task VerboseAsync(string source, string message, Exception exception = null)
        => LogAsync(new LogMessage(LogSeverity.Verbose, source, message, exception));

        public static Task DebugAsync(string source, string message, Exception exception = null)
        => LogAsync(new LogMessage(LogSeverity.Critical, source, message, exception));
    }
}
