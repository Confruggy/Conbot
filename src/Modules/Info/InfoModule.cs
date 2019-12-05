using System.Threading.Tasks;
using Discord.Commands;

namespace Conbot.Modules.Info
{
    [Name("Info")]
    public class InfoModule : ModuleBase
    {
        [Command("ping")]
        [Summary("Sents a message and shows the time difference between the command message and the bots response.")]
        [Remarks("This has nothing to do with your own ping.")]
        public async Task PingAsync()
        {
            var msg = await ReplyAsync("**Pong:** ...");
            long difference = (msg.CreatedAt - Context.Message.CreatedAt).Milliseconds;
            await ReplyAsync($"**Pong:** {difference} ms");
        } 
    }
}