using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Conbot.Commands;
using Conbot.Extensions;

using Discord;
using Discord.Net;
using Discord.WebSocket;

using Humanizer;

using Qmmands;

namespace Conbot.ModerationPlugin
{
    [Name("Moderation")]
    [Description("Moderation related commands.")]
    public class ModerationModule : DiscordModuleBase
    {
        [Command("prune", "purge", "clean", "clear")]
        [Description("Deletes messages in a channel.")]
        [Remarks("Only up to 100 of the latest 1.000 messages in the executing channel will be deleted.")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task PruneAsync(
            [Description("The member to delete messages from.")] IGuildUser? member = null,
            [Description("The maximal amount of messages to delete."), MinValue(1), MaxValue(100)] int limit = 10)
        {
            int count = await DeleteMessagesAsync(msg => member == null || msg.Author.Id == member.Id, limit);

            var text = new StringBuilder()
                .Append("messages".ToQuantity(count, Format.Bold("#")));

            if (member != null)
            {
                text
                    .Append(" from ")
                    .Append(member.Mention);
            }

            text
                .Append(' ')
                .Append("ha")
                .Append(count == 1 ? "s" : "ve")
                .Append(" been deleted.");

            var message = await ReplyAsync(text.ToString());
            await Task.Delay(10000);
            await message.TryDeleteAsync();
        }

        public async ValueTask<int> DeleteMessagesAsync(Func<IMessage, bool> condition, int limit = 100)
        {
            var channel = (SocketTextChannel)Context.Channel;

            var messages = new List<IMessage>();
            int count = 0;
            var tasks = new List<Task>();
            var now = DateTimeOffset.UtcNow;
            ulong minimum = SnowflakeUtils.ToSnowflake(now.Subtract(new TimeSpan(13, 23, 59, 0)));
            await Context.Channel.GetMessagesAsync(1000)
                .ForEachAsync(x =>
                {
                    foreach (var msg in x)
                    {
                        if (count >= limit || msg.Id <= minimum)
                            break;

                        if (!condition(msg))
                            continue;

                        messages.Add(msg);
                        count++;

                        if (count % 100 != 0)
                            continue;

                        tasks.Add(channel.DeleteMessagesAsync(messages));
                        messages = new List<IMessage>();
                    }
                });
            tasks.Add(channel.DeleteMessagesAsync(messages));
            await Task.WhenAll(tasks);
            return count;
        }

        [Command("ban")]
        [Description("Bans a member.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task BanAsync(
            [Description("The member to ban.")]
            [LowerHierarchy]
            SocketGuildUser member,
            [Name("prune days")]
            [Description("The amount of days to prune messages from the member.")]
            [MinValue(0), MaxValue(7)]
            int pruneDays = 0,
            [Description("The reason for the ban.")]
            [Remarks("The reason will show up in the audit log.")]
            [Remainder]
            string? reason = null)
        {
            if (member.Id == Context.User.Id)
            {
                await ReplyAsync("You can't ban yourself.");
                return;
            }

            await Context.Guild!.AddBanAsync(member, pruneDays, reason);
            await ReplyAsync($"**{Format.Sanitize(member.ToString())}** has been banned.");
        }

        [Command("hackban")]
        [Description("Bans a user by ID.")]
        [Remarks("This is useful to ban a user that isn't in the server.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task HackbanAsync(
            [Description("The ID of the user to ban.")]
            ulong id,
            [Name("prune days")]
            [Description("The amount of days to prune messages from the user.")]
            [MinValue(0), MaxValue(7)]
            int pruneDays = 0,
            [Description("The reason for the ban.")]
            [Remarks("The reason will show up in the audit log.")]
            [Remainder]
            string? reason = null)
        {
            if (id == Context.User.Id)
            {
                await ReplyAsync("You can't ban yourself.");
                return;
            }

            try
            {
                await Context.Guild!.AddBanAsync(id, pruneDays, reason);
                await ReplyAsync($"User with ID **{id}** has been banned.");
            }
            catch (HttpException e)
            {
                if (e.HttpCode == HttpStatusCode.NotFound)
                {
                    await ReplyAsync("User hasn't been found.");
                    return;
                }

                await ReplyAsync("The bot isn't able to ban this user.");
            }
        }

        [Command("unban")]
        [Description("Revokes a ban from a user.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task UnbanAsync(
            [Description("The user to revoke the ban from.")] SocketGuildUser user)
        {
            try
            {
                await Context.Guild!.RemoveBanAsync(user);
                await ReplyAsync($"**{Format.Sanitize(user.ToString())}** has been unbanned.");
            }
            catch (HttpException)
            {
                await ReplyAsync("User hasn't been found in the ban list.");
            }
        }

        [Command("softban")]
        [Description("Soft bans a member.")]
        [Remarks(
            "A soft ban is like a kick but instead kicking the member, " +
            "the member will be banned and directly unbanned. " +
            "This is useful for pruning messages from the member.")]
        [RequireUserPermission(GuildPermission.BanMembers, GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task SoftBanAsync(
            [Description("The member to soft ban.")]
            [LowerHierarchy]
            SocketGuildUser member,
            [Name("prune days")]
            [Description("The amount of days to prune messages from the member.")]
            [MinValue(0), MaxValue(7)]
            int pruneDays = 0,
            [Description("The reason for the soft ban.")]
            [Remarks("The reason will show up in the audit log.")]
            [Remainder]
            string? reason = null)
        {
            if (member.Id == Context.User.Id)
            {
                await ReplyAsync("You can't soft ban yourself.");
                return;
            }

            await Context.Guild!.AddBanAsync(member, pruneDays, reason);
            await Context.Guild.RemoveBanAsync(member);

            await ReplyAsync($"**{Format.Sanitize(member.ToString())}** has been soft banned.");
        }

        [Command("kick")]
        [Description("Kicks a member.")]
        [RequireUserPermission(GuildPermission.BanMembers, GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public async Task KickAsync(
            [Description("The member to kick.")]
            [LowerHierarchy]
            SocketGuildUser user,
            [Description("The reason for the kick.")]
            [Remarks("The reason will show up in the audit log.")]
            [Remainder]
            string? reason = null)
        {
            if (user.Id == Context.User.Id)
            {
                await ReplyAsync("You can't kick yourself.");
                return;
            }

            await user.KickAsync(reason);
            await ReplyAsync($"**{Format.Sanitize(user.ToString())}** has been kicked.");
        }
    }
}
