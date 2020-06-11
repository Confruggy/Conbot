using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Qmmands;

namespace Conbot.Commands
{
    public class DiscordModuleBase : ModuleBase<DiscordCommandContext>
    {
        public Task<RestUserMessage> ReplyAsync(string text = null, bool isTTS = false, Embed embed = null,
            RequestOptions options = null)
            => Context.Channel.SendMessageAsync(text, isTTS, embed, options);
    }
}