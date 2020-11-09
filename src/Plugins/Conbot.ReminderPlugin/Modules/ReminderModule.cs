using System.Text;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.TimeZonePlugin;
using Conbot.TimeZonePlugin.Extensions;
using Qmmands;

namespace Conbot.ReminderPlugin
{
    [Name("Reminder")]
    [Description("Create scheduled reminders.")]
    [Group("reminder", "remind", "timer")]
    public class ReminderModule : DiscordModuleBase
    {
        private readonly ReminderContext _db;

        public ReminderModule(ReminderContext context)
        {
            _db = context;
        }

        [Command]
        [Description("Reminds you about something after a certain time.")]
        [RequireTimeZone]
        public async Task ReminderAsync(
            [Description("The time when you want to be reminded. You can also set a message optionally.")]
            [Remarks(
                "The input can be either a human readable time and/or date or a duration. " +
                "However, you can't combine both. If a date has been set without a time, the time will default to 12 pm.\n\n" +
                "Examples for times and dates:\n" +
                "• \"at 7:30 wake up\"\n" +
                "• \"on the 19th April wish them a happy birthday\"\n" +
                "• \"next monday at 8pm watch the new episode\"\n\n" +
                "Examples for durations:\n" +
                "• \"in 20 minutes remove pizza from oven\"\n" +
                "• \"in 5 hours 30 min do laundry\"\n" +
                "• \"2h unmute someone\"")]
            [Remainder]
            ZonedDateTimeParseResult when)
        {
            if (when.Then.Value.LocalDateTime < when.Now.LocalDateTime)
            {
                await ReplyAsync("This time is in the past.");
                return;
            }

            if (when.Remainder?.StartsWith(' ') == false)
            {
                await ReplyAsync("There must be a space between the time and the message.");
                return;
            }

            await _db.AddReminderAsync(Context, when.Now.ToDateTimeUtc(), when.Then.Value.ToDateTimeUtc(),
                when.Remainder?[1..]);

            var text = new StringBuilder()
                .Append("⏰ Reminder has been set. You'll be reminded ")
                .Append(when.Now.ToDurationFormattedString(when.Then.Value))
                .Append(".");

            await Task.WhenAll(
                ReplyAsync(text.ToString()),
                _db.SaveChangesAsync()
            );
        }
    }
}