using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conbot.Commands;
using Discord;

namespace Conbot.ReminderPlugin
{
    public static class ReminderExtensions
    {
        public static ValueTask<List<Reminder>> GetRemindersAsync(this ReminderContext context, ulong? userId = null)
        {
            if (userId != null)
                return context.Reminders.AsAsyncEnumerable().Where(x => x.UserId == userId).ToListAsync();

            return context.Reminders.ToListAsync();
        }

        public static ValueTask<List<Reminder>> GetRemindersAsync(this ReminderContext context, IUser user)
            => GetRemindersAsync(context, user.Id);

        public static ValueTask<Reminder> GetReminderAsync(this ReminderContext context, int id)
            => context.Reminders.FirstOrDefaultAsync(x => x.Id == id);

        public static async ValueTask<Reminder> AddReminderAsync(this ReminderContext context, ulong userId,
            ulong? guildId, ulong channelId, ulong messageId, DateTime createdAt, DateTime endsAt,
            string message = null)
        {
            var reminder = new Reminder
            {
                UserId = userId,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                CreatedAt = createdAt,
                EndsAt = endsAt,
                Message = message
            };

            await context.Reminders.AddAsync(reminder);
            return reminder;
        }

        public static ValueTask<Reminder> AddReminderAsync(this ReminderContext db, DiscordCommandContext context,
            DateTime createdAt, DateTime endsAt, string message = null)
            => AddReminderAsync(
                    db, context.User.Id, context.Guild?.Id, context.Channel.Id,
                    context.Message?.Id ?? context.Interaction.Id , createdAt, endsAt, message);

        public static void RemoveReminder(this ReminderContext context, Reminder reminder)
            => context.Reminders.Remove(reminder);
    }
}