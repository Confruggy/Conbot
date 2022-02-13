using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Conbot.TimeZonePlugin;

using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

using Humanizer;

namespace Conbot.ReminderPlugin;

public class ReminderService : DiscordBotService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public ReminderService(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            await using var reminderContext = scope.ServiceProvider.GetRequiredService<ReminderContext>();
            await using var timeZoneContext = scope.ServiceProvider.GetRequiredService<TimeZoneContext>();

            var reminders = await reminderContext.Reminders.ToAsyncEnumerable()
                .Where(x => !x.Finished && x.EndsAt < DateTime.UtcNow)
                .ToArrayAsync(cancellationToken);

            foreach (var reminder in reminders)
                reminder.Finished = true;

            if (reminders.Length > 0)
                await reminderContext.SaveChangesAsync(cancellationToken);

            var tasks = new List<Task>();

            foreach (var reminder in reminders)
            {
                IMessageChannel? toSendChannel = null;

                if (reminder.GuildId is not null)
                {
                    toSendChannel = Bot.GetChannel(reminder.GuildId.Value, reminder.ChannelId) as IMessageChannel;

                    if (toSendChannel is IGuildChannel guildChannel)
                    {
                        var botUser = Bot.GetMember(reminder.GuildId.Value, Bot.CurrentUser.Id);

                        if (!botUser.GetPermissions(guildChannel).SendMessages)
                            toSendChannel = null;
                    }
                }

                if (toSendChannel is null)
                {
                    var user = Bot.GetUser(reminder.UserId);

                    if (user is not null)
                        toSendChannel = await user.CreateDirectChannelAsync(cancellationToken: cancellationToken);
                }

                if (toSendChannel is null)
                    continue;

                string time = Markdown.Timestamp(reminder.CreatedAt, Markdown.TimestampFormat.RelativeTime);

                string text;
                LocalMessageReference? reference = null;

                if (toSendChannel.Id == reminder.ChannelId)
                {
                    var originalMessage = await toSendChannel
                        .FetchMessageAsync(reminder.MessageId, cancellationToken: cancellationToken);

                    if (originalMessage is IUserMessage)
                    {
                        text = $"You've set a reminder {time}.";
                        reference = new LocalMessageReference
                        {
                            MessageId = reminder.MessageId,
                            ChannelId = reminder.ChannelId,
                            GuildId = reminder.GuildId
                        };
                    }
                    else
                    {
                        text = $"{Mention.User(reminder.UserId)}, you've set a reminder {time}.";
                    }
                }
                else
                {
                    text = $"You've set a reminder in {Mention.Channel(reminder.ChannelId)} {time}.";
                }

                LocalEmbed? embed;
                if (reference is not null)
                {
                    embed = !string.IsNullOrEmpty(reminder.Message)
                        ? new LocalEmbed()
                            .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                            .WithDescription(reminder.Message)
                        : null;
                }
                else
                {
                    string hyperLink = $"[jump to message]({reminder.Url})";

                    embed = new LocalEmbed()
                        .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                        .WithDescription(
                            string.IsNullOrEmpty(reminder.Message)
                                ? hyperLink
                                : $"{reminder.Message.Truncate(2021 - hyperLink.Length)} ({hyperLink})");
                }

                var message = new LocalMessage()
                    .WithContent(text);

                if (embed is not null)
                    message.AddEmbed(embed);

                if (reference is not null)
                    message.WithReference(reference);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await toSendChannel.SendMessageAsync(message, cancellationToken: cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Failed sending reminder message");
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(100, cancellationToken);
        }
    }
}