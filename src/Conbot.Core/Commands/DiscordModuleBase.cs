using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Qmmands;

namespace Conbot.Commands
{
    public class DiscordModuleBase : ModuleBase<DiscordCommandContext>
    {
        public Task<RestUserMessage> SendMessageAsync(string text = null, bool isTTS = false, Embed embed = null,
            RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference reference = null)
            => Context.Channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, reference);

        public async Task<RestUserMessage> ReplyAsync(string text = null, bool isTTS = false, Embed embed = null,
            AllowedMentions allowedMentions = null, RequestOptions options = null)
            => (RestUserMessage)await Context.Message.ReplyAsync(
                    text, isTTS, embed, allowedMentions ?? AllowedMentions.None, options);
    }
}