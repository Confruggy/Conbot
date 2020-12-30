using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Extensions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Qmmands;

namespace Conbot.Interactive
{
    public class InteractiveService : IHostedService
    {
        private readonly ConcurrentDictionary<ulong, ExecutingInteractiveMessage> _interactiveMessages;
        private readonly DiscordShardedClient _discordClient;
        private readonly CommandService _commandService;

        public InteractiveService(DiscordShardedClient discordClient, CommandService commandService)
        {
            _discordClient = discordClient;
            _commandService = commandService;
            _interactiveMessages = new ConcurrentDictionary<ulong, ExecutingInteractiveMessage>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _commandService.AddArgumentParser(new InteractiveArgumentParser());

            _discordClient.ReactionAdded += OnReactionAddedAsync;
            _discordClient.MessageReceived += OnMessageReceivedAsync;
            _discordClient.MessageDeleted += OnMessageDeletedAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discordClient.ReactionAdded -= OnReactionAddedAsync;
            _discordClient.MessageReceived -= OnMessageReceivedAsync;
            _discordClient.MessageDeleted -= OnMessageDeletedAsync;
            return Task.CompletedTask;
        }

        public async Task ExecuteInteractiveMessageAsync(InteractiveMessage interactiveMessage, IUserMessage message,
            IUser user)
        {
            if (_interactiveMessages.TryGetValue(user.Id, out var executingInteractiveMessage))
                StopExecutingInteractiveMessage(executingInteractiveMessage);

            executingInteractiveMessage = new ExecutingInteractiveMessage
            {
                InteractiveMessage = interactiveMessage,
                Message = message,
                User = user,
                TimeoutDate = DateTimeOffset.UtcNow.AddMilliseconds(interactiveMessage.Timeout),
                TokenSource = new CancellationTokenSource()
            };

            if (!_interactiveMessages.TryAdd(user.Id, executingInteractiveMessage))
                return;

            var token = executingInteractiveMessage.TokenSource.Token;

            if (interactiveMessage.AutoReactEmotes)
            {
                foreach (string emote in interactiveMessage.ReactionCallbacks.Keys)
                {
                    if (token.IsCancellationRequested)
                        break;
                    await message.TryAddReactionAsync(new Emoji(emote)).ConfigureAwait(false);
                }
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var timeout = executingInteractiveMessage.TimeoutDate - DateTimeOffset.UtcNow;
                    if (timeout.Ticks >= 0)
                        await Task.Delay(timeout, executingInteractiveMessage.TokenSource.Token).ConfigureAwait(false);
                    if (DateTimeOffset.UtcNow >= executingInteractiveMessage.TimeoutDate)
                    {
                        StopExecutingInteractiveMessage(executingInteractiveMessage);
                        break;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            await message.TryRemoveAllReactionsAsync();
        }

        private Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Run(async () =>
            {
                foreach (var executingInteractiveMessage in _interactiveMessages.Values)
                {
                    if (executingInteractiveMessage.Message.Id != msg.Id)
                        return;
                    if (!reaction.User.IsSpecified)
                        return;
                    if (reaction.UserId == _discordClient.CurrentUser.Id)
                        return;

                    var interactiveMessage = executingInteractiveMessage.InteractiveMessage;

                    var emote = reaction.Emote;
                    string emojiString = emote is Emote e ? $"{e.Name}:{e.Id}" : emote.Name;

                    var user = reaction.User.Value;
                    if (interactiveMessage.Precondition != null &&
                        !await interactiveMessage.Precondition(user).ConfigureAwait(false))
                    {
                        return;
                    }

                    if (!interactiveMessage.ReactionCallbacks.TryGetValue(emojiString, out var callback))
                        return;

                    executingInteractiveMessage.TimeoutDate =
                        DateTimeOffset.UtcNow.AddMilliseconds(interactiveMessage.Timeout);

                    try
                    {
                        await callback.Callback(reaction).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (callback.ResumeAfterExecution)
                        {
                            await executingInteractiveMessage.Message
                               .TryRemoveReactionAsync(emote, user).ConfigureAwait(false);
                        }
                        else
                        {
                            StopExecutingInteractiveMessage(executingInteractiveMessage);
                        }
                    }
                }
            });
            return Task.CompletedTask;
        }

        private Task OnMessageReceivedAsync(IMessage msg)
        {
            Task.Run(async () =>
            {
                if (!(msg is IUserMessage userMessage))
                    return;

                foreach (var executingInteractiveMessage in _interactiveMessages.Values)
                {
                    if (msg.Channel.Id != executingInteractiveMessage.Message.Channel.Id)
                        continue;

                    var interactiveMessage = executingInteractiveMessage.InteractiveMessage;

                    if (interactiveMessage.Precondition != null &&
                        !await interactiveMessage.Precondition(msg.Author).ConfigureAwait(false))
                    {
                        continue;
                    }

                    MessageCallback callback = null;

                    foreach (var messageCallback in interactiveMessage.MessageCallbacks)
                    {
                        if (messageCallback.Precondition == null)
                        {
                            callback = messageCallback;
                            break;
                        }

                        if (await messageCallback.Precondition.Invoke(userMessage).ConfigureAwait(false))
                        {
                            callback = messageCallback;
                            break;
                        }
                    }

                    if (callback == null)
                        return;

                    executingInteractiveMessage.TimeoutDate =
                        DateTime.UtcNow.AddMilliseconds(interactiveMessage.Timeout);

                    try
                    {
                        await callback.Callback(userMessage).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (!callback.ResumeAfterExecution)
                            StopExecutingInteractiveMessage(executingInteractiveMessage);
                    }
                }
            });
            return Task.CompletedTask;
        }

        private Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel)
        {
            if (!_interactiveMessages.TryGetValue(msg.Id, out var executingInteractiveMessage))
                return Task.CompletedTask;
            if (msg.Id != executingInteractiveMessage.Message.Id)
                return Task.CompletedTask;

            StopExecutingInteractiveMessage(executingInteractiveMessage);
            return Task.CompletedTask;
        }

        private void StopExecutingInteractiveMessage(ExecutingInteractiveMessage executingInteractiveMessage)
        {
            executingInteractiveMessage.TokenSource.Cancel(true);
            _interactiveMessages.TryRemove(executingInteractiveMessage.User.Id, out var _);
        }
    }
}