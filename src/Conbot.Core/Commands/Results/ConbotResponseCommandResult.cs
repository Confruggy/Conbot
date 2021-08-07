using System.Threading.Tasks;

using Disqord;
using Disqord.Bot;

namespace Conbot.Commands
{
    public class ConbotResponseCommandResult : DiscordResponseCommandResult
    {
        public ConbotResponseCommandResult(ConbotCommandContext context, LocalMessage message)
            : base(context, message)
        { }

        public override async Task<IUserMessage?> ExecuteAsync()
        {
            var message = await base.ExecuteAsync();

            var context = (ConbotCommandContext)Context;

            if (message is not null)
                context.AddMessage(message);

            return message;
        }
    }
}
