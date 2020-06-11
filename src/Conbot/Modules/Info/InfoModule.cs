using System.Threading.Tasks;
using Conbot.Commands;
using Qmmands;

namespace Conbot.Modules.Info
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
            long difference = (msg.CreatedAt - Context.Message.CreatedAt).Milliseconds;
            await msg.ModifyAsync(x => x.Content = $"**Pong:** {difference} ms");
        } 
    }
}