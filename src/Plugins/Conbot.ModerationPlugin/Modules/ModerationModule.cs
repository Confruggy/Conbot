using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Conbot.Commands;
using Conbot.Extensions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Qmmands;

namespace Conbot.ModerationPlugin
{
    [Name("Moderation")]
    [Description("Moderation related commands.")]
    public class ModerationModule : DiscordModuleBase
    {
        [Group("prune", "purge", "clean", "clear")]
        [Description("Deletes an amount of messages.")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public class PruneCommands : DiscordModuleBase
        {
            [Command]
            [Description("Deletes a specific amount of messages.")]
            [Remarks("Only up to 100 of the latest 1.000 messages in the executing channel will be deleted.")]
            [Priority(1)]
            public async Task PruneAsync([Description("The maximal amount of messages to delete.")] uint limit)
            {
                int count = await DeleteMessagesAsync(_ => true, limit < 100 ? limit : 100);

                var message = await ReplyAsync($"**{count}** messages has been deleted.");
                await Task.Delay(10000);
                await message.TryDeleteAsync();
            }

            [Command]
            [Description("Deletes a specifc amount of messages of a specific member.")]
            [Remarks("Only up to 100 of the latest 1.000 messages in the executing channel will be deleted.")]
            [Priority(0)]
            public async Task PruneAsync(
                [Description("The member to delete messages from.")] IGuildUser member,
                [Description("The maximal amount of messages to delete.")] uint limit = 100)
            {
                int count = await DeleteMessagesAsync(msg => msg.Author.Id == member.Id, limit < 100 ? limit : 100);
                var message = member != null
                    ? await ReplyAsync($"**{count}** messages from {member.Mention} has been deleted.")
                    : await ReplyAsync($"**{count}** messages has been deleted.");
                await Task.Delay(10000);
                await message.TryDeleteAsync();
            }

            public async Task<int> DeleteMessagesAsync(Func<IMessage, bool> condition, uint limit = 100)
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
        }

        [Command("ban")]
        [Description("Bans a member.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(
            [Description("The member to ban."), LowerHierarchy] SocketGuildUser member,
            [Name("prune days")]
            [Description("The amount of days to prune messages from the member.")]
            [MinValue(0), MaxValue(7)] int prunedays = 0)
        {
            if (member.Id == Context.User.Id)
            {
                await ReplyAsync("You can't ban yourself.");
                return;
            }

            await Context.Guild.AddBanAsync(member, prunedays);
            await ReplyAsync($"**{Format.Sanitize(member.ToString())}** has been banned.");
        }

        [Command("hackban")]
        [Description("Bans a user by ID.")]
        [Remarks("This is useful to ban a user that isn't in the server.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task HackbanAsync(
            [Description("The ID of the user to ban.")] ulong id,
            [Name("prune days")]
            [Description("The amount of days to prune messages from the user.")]
            [MinValue(0), MaxValue(7)] int prunedays = 0)
        {
            if (id == Context.User.Id)
            {
                await ReplyAsync("You can't ban yourself.");
                return;
            }

            try
            {
                await Context.Guild.AddBanAsync(id, prunedays);
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
        public async Task UnbanAsync(
            [Description("The user to revoke the ban from.")] SocketGuildUser user)
        {
            try
            {
                await Context.Guild.RemoveBanAsync(user);
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
            "This is useful for pruning the messages of the member.")]
        [RequireUserPermission(GuildPermission.BanMembers, GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task SoftBanAsync(
            [Description("The member to soft ban."), LowerHierarchy] SocketGuildUser member,
            [Name("prune days")]
            [Description("The amount of days to prune messages from the member.")]
            [MinValue(0), MaxValue(7)] int prunedays = 1)
        {
            if (member.Id == Context.User.Id)
            {
                await ReplyAsync("You can't soft ban yourself.");
                return;
            }

            await Context.Guild.AddBanAsync(member, prunedays);
            await Context.Guild.RemoveBanAsync(member);

            await ReplyAsync($"**{Format.Sanitize(member.ToString())}** has been soft banned.");
        }

        [Command("kick")]
        [Description("Kicks a member.")]
        [RequireUserPermission(GuildPermission.BanMembers, GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(
            [Description("The member to kick."), LowerHierarchy] SocketGuildUser user)
        {
            if (user.Id == Context.User.Id)
            {
                await ReplyAsync("You can't kick yourself.");
                return;
            }

            await user.KickAsync();
            await ReplyAsync($"**{Format.Sanitize(user.ToString())}** has been kicked.");
        }
    }
}