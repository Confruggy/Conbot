using System;
using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;
using Disqord.Rest;

namespace Conbot.Commands
{
    public class ConbotModifyCommandResult : DiscordCommandResult
    {
        private readonly Action<ModifyMessageActionProperties> _action;

        public IUserMessage Message { get; }

        public ConbotModifyCommandResult(DiscordCommandContext context, IUserMessage message, string newContent)
            : base(context)
        {
            Message = message;

            _action = (properties) => properties.Content = newContent;
        }

        public ConbotModifyCommandResult(DiscordCommandContext context, IUserMessage message,
            params LocalEmbed[] newEmbeds)
            : base(context)
        {
            Message = message;

            _action = (properties) => properties.Embeds = newEmbeds;
        }

        public ConbotModifyCommandResult(DiscordCommandContext context, IUserMessage message, string newContent,
            params LocalEmbed[] newEmbeds)
            : base(context)
        {
            Message = message;

            _action = (properties) =>
            {
                properties.Content = newContent;
                properties.Embeds = newEmbeds;
            };
        }

        public override Task ExecuteAsync()
            => Message.ModifyAsync(_action);
    }
}