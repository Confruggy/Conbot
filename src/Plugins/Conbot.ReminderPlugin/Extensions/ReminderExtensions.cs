using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Disqord;
using Disqord.Bot;

namespace Conbot.ReminderPlugin;

public static class ReminderExtensions
{
    public static IAsyncEnumerable<Reminder> GetRemindersAsync(this ReminderContext context, ulong? userId = null)
    {
        if (userId is not null)
            return context.Reminders.AsQueryable().Where(x => x.UserId == userId).AsAsyncEnumerable();

        return context.Reminders.AsAsyncEnumerable();
    }

    public static IAsyncEnumerable<Reminder> GetRemindersAsync(this ReminderContext context, IUser user)
        => GetRemindersAsync(context, user.Id);

    public static async Task<Reminder?> GetReminderAsync(this ReminderContext context, int id)
        => await context.Reminders.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);

    public static async Task<Reminder> AddReminderAsync(this ReminderContext context, ulong userId,
        ulong? guildId, ulong channelId, ulong messageId, DateTime createdAt, DateTime endsAt,
        string? message = null)
    {
        var reminder = new Reminder(userId, guildId, channelId, messageId, message, createdAt, endsAt);
        await context.Reminders.AddAsync(reminder);
        return reminder;
    }

    public static Task<Reminder> AddReminderAsync(this ReminderContext db, DiscordCommandContext context,
        DateTime createdAt, DateTime endsAt, string? message = null)
        => AddReminderAsync(db, context.Author.Id, context.GuildId, context.ChannelId, context.Message.Id,
            createdAt, endsAt, message);

    public static void RemoveReminder(this ReminderContext context, Reminder reminder)
        => context.Reminders.Remove(reminder);
}