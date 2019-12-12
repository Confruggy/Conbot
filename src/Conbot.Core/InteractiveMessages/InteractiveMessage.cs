using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Conbot.Extensions;
using Discord;
using Discord.WebSocket;

namespace Conbot.InteractiveMessages
{
    public class InteractiveMessage
    {
        private static readonly SemaphoreSlim _reactionLock = new SemaphoreSlim(1, 1);
        public Func<IUser, Task<bool>> Precondition { get; }
        public int Timeout { get; } = 60000;

        public Dictionary<string, ReactionCallback> ReactionCallbacks { get; }
            = new Dictionary<string, ReactionCallback>();

        public List<MessageCallback> MessageCallbacks { get; }
            = new List<MessageCallback>();

        public bool AutoReactEmotes { get; }

        public InteractiveMessage(Func<IUser, Task<bool>> precondition, int timeout,
            Dictionary<string, ReactionCallback> reactionCallbacks,
            List<MessageCallback> messageCallbacks, bool autoReactEmotes)
        {
            Precondition = precondition;
            Timeout = timeout;
            ReactionCallbacks = reactionCallbacks;
            MessageCallbacks = messageCallbacks;
            AutoReactEmotes = autoReactEmotes;
        }

        public Task ExecuteAsync(DiscordShardedClient client, IUserMessage message)
        {
            DiscordSocketClient socketClient;
            if (message.Channel is IGuildChannel guildChannel)
                socketClient = client.GetShardFor(guildChannel.Guild);
            else socketClient = client.GetShard(0); //shard for DMs
            return ExecuteAsync(socketClient, message);
        }

        public async Task ExecuteAsync(DiscordSocketClient client, IUserMessage message)
        {
            var tokenSource = new CancellationTokenSource();
            var timeoutDate = DateTime.UtcNow.AddMilliseconds(Timeout);
            bool isFinished = false;

            Task onReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
            {
                Task.Run(async () =>
                {
                    if (msg.Id != message.Id || !reaction.User.IsSpecified)
                        return;
                    if (reaction.UserId == client.CurrentUser.Id)
                        return;

                    var emote = reaction.Emote;
                    string emojiString = emote is Emote e ? $"{e.Name}:{e.Id}" : emote.Name;

                    var user = reaction.User.Value;
                    if (Precondition != null && !await Precondition(user).ConfigureAwait(false))
                        return;
                    if (!ReactionCallbacks.TryGetValue(emojiString, out var callback))
                        return;

                    timeoutDate = DateTime.UtcNow.AddMilliseconds(Timeout);

                    try
                    {
                        await callback.Callback(reaction).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (callback.ResumeAfterExecution)
                        {
                            await _reactionLock.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                if (client.CurrentUser.IsBot)
                                    await message.RemoveReactionAsync(emote, user).ConfigureAwait(false);
                                else await message.AddReactionAsync(emote).ConfigureAwait(false);
                            }
                            catch { }
                            finally
                            {
                                _reactionLock.Release();
                            }
                        }

                        else
                            tokenSource.Cancel(true);
                    }
                });
                return Task.CompletedTask;
            }

            Task onMessageReceived(IMessage msg)
            {
                Task.Run(async () =>
                {
                    if (!(msg is IUserMessage userMessage))
                        return;
                    if (msg.Channel.Id != message.Channel.Id)
                        return;
                    if (Precondition != null && !await Precondition(userMessage.Author).ConfigureAwait(false))
                        return;

                    MessageCallback callback = null;

                    foreach (var messageCallback in MessageCallbacks)
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

                    timeoutDate = DateTime.UtcNow.AddMilliseconds(Timeout);

                    try
                    {
                        await callback.Callback(userMessage).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (!callback.ResumeAfterExecution)
                            tokenSource.Cancel(true);
                    }
                });
                return Task.CompletedTask;
            }

            Task onMessageDeleted(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel)
            {
                if (msg.Id == message.Id)
                    tokenSource.Cancel(true);
                return Task.CompletedTask;
            }

            client.ReactionAdded += onReactionAdded;
            client.MessageReceived += onMessageReceived;
            client.MessageDeleted += onMessageDeleted;

            foreach (string emote in ReactionCallbacks.Keys)
            {
                await _reactionLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    await message.TryAddReactionAsync(new Emoji(emote)).ConfigureAwait(false);
                }
                finally
                {
                    _reactionLock.Release();
                }
            }

            do
            {
                try
                {
                    var timeout = timeoutDate - DateTime.UtcNow;
                    if (timeout.Ticks >= 0)
                        await Task.Delay(timeout, tokenSource.Token).ConfigureAwait(false);
                    if (DateTime.UtcNow >= timeoutDate)
                        isFinished = true;
                }
                catch (TaskCanceledException)
                {
                    isFinished = true;
                }
            } while (!isFinished);

            client.ReactionAdded -= onReactionAdded;
            client.MessageReceived -= onMessageReceived;
            client.MessageDeleted -= onMessageDeleted;

            try
            {
                await message.RemoveAllReactionsAsync();
            }
            catch { }
        }
    }
}
