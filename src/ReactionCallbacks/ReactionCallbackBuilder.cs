using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Threading;
using Discord;
using Conbot.Extensions;

namespace Conbot.ReactionCallbacks
{
    public class ReactionCallbackBuilder
    {
        public static SemaphoreSlim _reactionLock = new SemaphoreSlim(1, 1);

        public Func<IUser, Task<bool>> Precondition { get; set; }

        public int Timeout { get; set; } = 60000;

        public Dictionary<string, ReactionCallback> Callbacks { get; set; } = new Dictionary<string, ReactionCallback>();

        public bool AutoReactEmotes { get; set; }

        public ReactionCallbackBuilder WithAutoReactEmotes(bool @value = true)
        {
            AutoReactEmotes = @value;
            return this;
        }

        public ReactionCallbackBuilder WithPrecondition(Func<IUser, Task<bool>> func)
        {
            Precondition = func;
            return this;
        }

        public ReactionCallbackBuilder WithTimeout(int ms)
        {
            Timeout = ms;
            return this;
        }

        public ReactionCallbackBuilder WithPrecondition(Func<IUser, bool> func)
        {
            Precondition = x => Task.FromResult(func(x));
            return this;
        }

        public ReactionCallbackBuilder AddCallback(string emoji, Func<IUser, Task> callback, bool resumeAfterExecution = false)
        {
            Callbacks.Add(emoji, new ReactionCallback { Function = callback, ResumeAfterExecution = resumeAfterExecution });
            return this;
        }

        public ReactionCallbackBuilder AddCallback(string emoji, Action<IUser> callback, bool resumeAfterExecution = false)
        {
            Callbacks.Add(emoji, new ReactionCallback { Function = x => { callback(x); return Task.CompletedTask; }, ResumeAfterExecution = resumeAfterExecution });
            return this;
        }

        public Task ExecuteAsync(IDiscordClient client, IUserMessage message)
        {
            if (client is DiscordShardedClient shardedClient)
                return ExecuteAsync(shardedClient, message);
            else if (client is DiscordSocketClient socketClient)
                return ExecuteAsync(socketClient, message);
            throw new InvalidOperationException("Not supported client");
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

            Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> onReactionAdded = (msg, channel, reaction) =>
            {
                Task.Run(async () =>
                {
                    if (msg.Id != message.Id || !reaction.User.IsSpecified)
                        return;
                    if (client.CurrentUser.IsBot && reaction.UserId == client.CurrentUser.Id)
                        return;

                    var emote = reaction.Emote;
                    string emojiString = emote is Emote e ? $"{e.Name}:{e.Id}" : emote.Name;

                    var user = reaction.User.Value;
                    if (!Callbacks.TryGetValue(emojiString, out var callback))
                        return;
                    if (Precondition != null && !await Precondition(user).ConfigureAwait(false))
                        return;
                    timeoutDate = DateTime.UtcNow.AddMilliseconds(Timeout);
                    try
                    {
                        await callback.Function(reaction.User.Value).ConfigureAwait(false);
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
            };

            Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task> onMessageDeleted = (msg, channel) =>
            {
                if (msg.Id == message.Id)
                    tokenSource.Cancel(true);
                return Task.CompletedTask;
            };

            if (client.CurrentUser.IsBot)
                client.ReactionAdded += onReactionAdded;
            else client.ReactionRemoved += onReactionAdded;
            client.MessageDeleted += onMessageDeleted;

            foreach (string emote in Callbacks.Keys)
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

            if (client.CurrentUser.IsBot)
                client.ReactionAdded -= onReactionAdded;
            else client.ReactionRemoved -= onReactionAdded;
            client.MessageDeleted -= onMessageDeleted;
            try
            {
                await message.RemoveAllReactionsAsync();
            }
            catch { }
        }
    }
}
