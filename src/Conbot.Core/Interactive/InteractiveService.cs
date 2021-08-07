using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Conbot.Commands;
using Conbot.Extensions;

using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

namespace Conbot.Interactive
{
    public class InteractiveService : DiscordBotService
    {
        private readonly ConcurrentDictionary<ulong, InteractiveUserMessage> _interactiveMessages;

        public InteractiveService()
        {
            _interactiveMessages = new ConcurrentDictionary<ulong, InteractiveUserMessage>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Bot.Commands.AddArgumentParser(new InteractiveArgumentParser());

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Bot.Commands.RemoveArgumentParser<InteractiveArgumentParser>();

            return base.StopAsync(cancellationToken);
        }

        public async ValueTask<IInteractiveUserMessage> ExecuteInteractiveMessageAsync(
            LocalInteractiveMessage interactiveMessage, ConbotCommandContext context)
        {
            if (_interactiveMessages.TryGetValue(context.Author.Id, out var interactiveUserMessage))
                StopInteractiveMessage(interactiveUserMessage);

            var message =
                (TransientUserMessage)await context.Bot.SendMessageAsync(context.ChannelId, interactiveMessage);

            context.AddMessage(message);

            interactiveUserMessage = new InteractiveUserMessage(message, interactiveMessage, context.Author, this);

            if (!_interactiveMessages.TryAdd(context.Author.Id, interactiveUserMessage))
                return interactiveUserMessage;

            var token = interactiveUserMessage.TokenSource.Token;

            foreach (var emoji in interactiveMessage.ReactionCallbacks.Values
                .Where(x => x.AutoReact)
                .Select(x => x.Emoji))
            {
                if (token.IsCancellationRequested)
                    break;

                await message.TryAddReactionAsync(emoji);
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var timeout = interactiveUserMessage.TimeoutsAt - DateTimeOffset.UtcNow;
                    if (timeout.Ticks >= 0)
                        await Task.Delay(timeout, interactiveUserMessage.TokenSource.Token);

                    if (DateTimeOffset.UtcNow >= interactiveUserMessage.TimeoutsAt)
                    {
                        StopInteractiveMessage(interactiveUserMessage);
                        break;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            await message.TryClearAllReactionsAsync();

            return interactiveUserMessage;
        }

        protected override async ValueTask OnReactionAdded(ReactionAddedEventArgs e)
        {
            foreach (var interactiveMessage in _interactiveMessages.Values)
            {
                if (interactiveMessage.Id != e.MessageId)
                    continue;

                if (e.UserId == Bot.CurrentUser.Id)
                    return;

                if (interactiveMessage.Precondition is not null && !await interactiveMessage.Precondition(e.Member))
                    return;

                var emoji = LocalEmoji.FromString(e.Emoji.GetReactionFormat());

                if (!interactiveMessage.ReactionCallbacks.TryGetValue(emoji, out var callback))
                    return;

                interactiveMessage.TimeoutsAt = DateTimeOffset.UtcNow.AddMilliseconds(interactiveMessage.Timeout);

                try
                {
                    await callback.Callback(interactiveMessage, e);
                }
                finally
                {
                    if (!interactiveMessage.TokenSource.IsCancellationRequested)
                        await interactiveMessage.TryRemoveReactionAsync(LocalEmoji.FromEmoji(emoji), e.Member.Id);
                }
            }
        }

        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
        {
            if (e.Message is not IUserMessage userMessage)
                return;

            foreach (var interactiveMessage in _interactiveMessages.Values)
            {
                if (e.ChannelId != interactiveMessage.ChannelId)
                    continue;

                if (interactiveMessage.Precondition is not null &&
                    !await interactiveMessage.Precondition(e.Member))
                {
                    continue;
                }

                MessageCallback? callback = null;

                foreach (var messageCallback in interactiveMessage.MessageCallbacks)
                {
                    if (messageCallback.Precondition is null)
                    {
                        callback = messageCallback;
                        break;
                    }

                    if (await messageCallback.Precondition.Invoke(interactiveMessage, e))
                    {
                        callback = messageCallback;
                        break;
                    }
                }

                if (callback is null)
                    return;

                interactiveMessage.TimeoutsAt = DateTime.UtcNow.AddMilliseconds(interactiveMessage.Timeout);

                await callback.Callback(interactiveMessage, e);
            }
        }

        protected override ValueTask OnMessageDeleted(MessageDeletedEventArgs e)
        {
            if (!_interactiveMessages.TryGetValue(e.MessageId, out var interactiveMessage))
                return ValueTask.CompletedTask;

            if (e.MessageId != interactiveMessage.Id)
                return ValueTask.CompletedTask;

            StopInteractiveMessage(interactiveMessage);
            return ValueTask.CompletedTask;
        }

        internal void StopInteractiveMessage(InteractiveUserMessage interactiveMessage)
        {
            interactiveMessage.TokenSource.Cancel(true);
            _interactiveMessages.TryRemove(interactiveMessage.User.Id, out var _);
        }
    }
}
