using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Conbot.Commands;
using Conbot.Extensions;

using Qmmands;

namespace Conbot.InfoPlugin
{
    [Name("Info")]
    public class InfoModule : DiscordModuleBase
    {
        [Command("ping")]
        [Description("Sents a message and shows the time difference between the command message and the bots response.")]
        [Remarks("This has nothing to do with your own ping.")]
        public async Task PingAsync()
        {
            var msg = await ReplyAsync("**Pong:** ...");
            long difference =
                (msg.CreatedAt - (Context.Interaction?.CreatedAt ?? Context.Message!.CreatedAt)).Milliseconds;
            await msg.ModifyAsync(x => x.Content = $"**Pong:** {difference} ms");
        }

        [Command("uptime")]
        [Description("Shows the uptime of the bot.")]
        public Task UptimeAsync() => ReplyAsync($"The bot is running for {GetUptime().ToLongFormattedString()}.");

        private static TimeSpan GetUptime()
            => DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime);
    }
}