using System.Threading;
using System.Threading.Tasks;

using Conbot.Commands;

using Discord.WebSocket;

namespace Conbot.Extensions
{
    public static class CommandContextExtensions
    {
        public static async Task<SocketUserMessage?> WaitForMessageAsync(this DiscordCommandContext context,
            int timeout = 60000, CancellationTokenSource? tokenSource = null)
        {
            tokenSource ??= new CancellationTokenSource();

            var client = context.Client;

            SocketUserMessage? message = null;

            Task Func(SocketMessage msg)
            {
                if (msg.Channel.Id == context.Channel.Id && msg.Author.Id == context.User.Id &&
                    msg is SocketUserMessage userMessage)
                {
                    message = userMessage;
                    tokenSource.Cancel(true);
                }

                return Task.CompletedTask;
            }

            client.MessageReceived += Func;

            try
            {
                await Task.Delay(timeout, tokenSource.Token);
            }
            catch
            {
                return message;
            }
            finally
            {
                client.MessageReceived -= Func;
            }

            return null;
        }
    }
}
