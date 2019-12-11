using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Conbot.Extensions
{
    public static class CommandContextExtensions
    {
        public static async Task<SocketUserMessage> WaitForMessageAsync(this SocketCommandContext context,
            int timeout = 60000, CancellationTokenSource tokenSource = null)
        {
            tokenSource ??= new CancellationTokenSource();

            var client = context.Client;

            SocketUserMessage message = null;

            Task func(SocketMessage msg)
            {
                if (msg.Channel.Id == context.Channel.Id && msg.Author.Id == context.User.Id &&
                    msg is SocketUserMessage userMessage)
                {
                    message = userMessage;
                    tokenSource.Cancel(true);
                }
                return Task.CompletedTask;
            }

            client.MessageReceived += func;

            try
            {
                await Task.Delay(timeout, tokenSource.Token).ConfigureAwait(false);
            }
            catch
            {
                return message;
            }
            finally
            {
                client.MessageReceived -= func;
            }

            return null;
        }
    }
}