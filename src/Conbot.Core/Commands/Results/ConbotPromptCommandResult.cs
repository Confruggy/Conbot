using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Conbot.Interactive;

using Disqord;
using Disqord.Bot;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Conbot.Commands
{
    public class ConbotPromptCommandResult : DiscordCommandResult
    {
        private readonly string _text;
        private readonly int _timeout;

        public bool? Result { get; private set; }

        public ConbotPromptCommandResult(DiscordCommandContext context, string text, int timeout = 60000)
            : base(context)
        {
            _text = text;
            _timeout = timeout;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new virtual TaskAwaiter<IInteractiveUserMessage> GetAwaiter()
            => ExecuteAsync().GetAwaiter();

        public override async Task<IInteractiveUserMessage> ExecuteAsync()
        {
            var interactiveService = Context.Services.GetRequiredService<InteractiveService>();
            var config = Context.Services.GetRequiredService<IConfiguration>();

            var interactiveMessage = new LocalInteractiveMessage()
                .WithContent(_text)
                .WithPrecondition(x => x.Id == Context.Author.Id)
                .WithTimeout(_timeout)
                .AddReactionCallback(config.GetValue<string>("Emotes:CheckMark"), x => x
                    .WithCallback((msg, _) =>
                    {
                        Result = true;
                        msg.Stop();
                    }))
                .AddReactionCallback(config.GetValue<string>("Emotes:CrossMark"), x => x
                    .WithCallback((msg, _) =>
                    {
                        Result = false;
                        msg.Stop();
                    }));

            return await interactiveService
                .ExecuteInteractiveMessageAsync(interactiveMessage, (ConbotCommandContext)Context);
        }
    }
}
