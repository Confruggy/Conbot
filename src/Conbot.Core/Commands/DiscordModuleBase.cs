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
            => Context.SendMessageAsync(text, isTTS, embed, options, allowedMentions, reference);

        public Task<RestUserMessage> ReplyAsync(string text = null, bool isTTS = false, Embed embed = null,
            AllowedMentions allowedMentions = null, RequestOptions options = null)
            => Context.ReplyAsync(text, isTTS, embed, allowedMentions ?? AllowedMentions.None, options);

        public Task<(RestUserMessage, bool?)> ConfirmAsync(string text, bool isTTS = false,
            Embed embed = null, AllowedMentions allowedMentions = null, RequestOptions options = null,
            int timeout = 60000)
            => Context.ConfirmAsync(text, isTTS, embed, allowedMentions, options, timeout);
    }
}