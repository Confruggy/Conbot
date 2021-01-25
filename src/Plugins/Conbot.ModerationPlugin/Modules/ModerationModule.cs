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
        private readonly ModerationContext _db;

        public ModerationModule(ModerationContext context) => _db = context;

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

        [Command("mute")]
        [Description("Mutes a member.")]
        [Remarks(
            "This will assign a \"muted role\" to the member. To use this command, the server must have a muted role " +
            "configured using the **mutedrole set** command.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.AddReactions | ChannelPermission.UseExternalEmojis)]
        public async Task MuteAsync(
            [Description("The member to mute.")]
            IGuildUser member,
            [Description("The duration for how long the member will be muted.")]
            [Remarks("If left blank, the member will be muted until someone manually unmutes them.")]
            [Remainder]
            TimeSpan? duration = null)
        {
            var config = await _db.GetGuildConfigurationAsync(Context.Guild!);
            IRole role;
            if (config?.RoleId is null || (role = Context.Guild.GetRole(config.RoleId.Value)) is null)
            {
                await ReplyAsync(
                    "There is no role for muting members configured. Please configure a muted role with the " +
                    "**mutedrole** command.");
                return;
            }

            if (member.Id == Context.User.Id)
            {
                await ReplyAsync("You can't mute yourself.");
                return;
            }

            if (member.Id == Context.Guild.CurrentUser.Id)
            {
                await ReplyAsync("You can't mute the bot.");
                return;
            }

            if (member.RoleIds.Contains(config.RoleId.Value))
            {
                var message = await ConfirmAsync("This member is already muted. Do you want to adjust the duration?");

                if (message.Item2 != true)
                {
                    await ReplyAsync("Duration for the mute wasn't adjusted.");
                    return;
                }
            }

            if (role.Position >= ((SocketGuildUser)Context.User).Hierarchy)
            {
                await ReplyAsync(
                    "You can't assign the muted role to members because its position is as high or higher than your " +
                    "highest role.");
                return;
            }

            if (role.Position >= Context.Guild.CurrentUser.Hierarchy)
            {
                await ReplyAsync(
                    "The muted role can't be assigned to members because its position is as high or higher than the " +
                    "bots highest role.");
                return;
            }

            string text;
            if (duration is null)
            {
                string reason = $"Muted by {Context.User} (ID: {Context.User.Id})";

                await Task.WhenAll(
                    member.AddRoleAsync(role, new RequestOptions { AuditLogReason = reason }),
                    _db.TryRemoveTemporaryMutedUserAsync(member)
                );

                text = $"{member.Mention} is now muted.";
            }
            else
            {
                string reason =
                    $"Temporarily muted for {duration.Value.ToLongString()} by {Context.User} (ID: {Context.User.Id})";

                var now = DateTime.UtcNow;

                await Task.WhenAll(
                    member.AddRoleAsync(role, new RequestOptions { AuditLogReason = reason }),
                    _db.CreateOrUpdateTemporaryMutedUserAsync(member, role, now, now.Add(duration.Value))
                );

                text = $"{member.Mention} is now temporarily muted for {duration.Value.ToLongFormattedString()}.";
            }

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync(text, allowedMentions: AllowedMentions.None)
            );
        }

        [Command("unmute")]
        [Description("Unmutes a muted member.")]
        [Remarks(
            "You can also unmute members by manually removing the muted role from them. However, there might be " +
            "cases where the bot doesn't detect the removal of a role (e.g. when the bot was offline at that time). " +
            "In those cases the bot might try to remove the role from the member after the mute duration expired " +
            "when the member was temporarily muted. So unmuting a member using this command is recommended. You can " +
            "use the **mutedmembers** command to check which members are currently temporarily muted.")]
        public async Task UnmuteAsync([Description("The member to unmute.")] IGuildUser member)
        {
            var config = await _db.GetGuildConfigurationAsync(Context.Guild!);
            IRole role;
            if (config?.RoleId is null || (role = Context.Guild.GetRole(config.RoleId.Value)) is null)
            {
                await ReplyAsync(
                    "There is no role for muting members configured. Please configure a muted role with the " +
                    "**mutedrole** command.");
                return;
            }

            if (!member.RoleIds.Contains(config.RoleId.Value))
            {
                await ReplyAsync("This member isn't muted.");
                return;
            }

            if (role.Position >= ((SocketGuildUser)Context.User).Hierarchy)
            {
                await ReplyAsync(
                    "You can't remove the muted role from members because its position is as high or higher than " +
                    "your highest role.");
                return;
            }

            if (role.Position >= Context.Guild.CurrentUser.Hierarchy)
            {
                await ReplyAsync(
                    "The muted role can't be removed from members because its position is as high or higher than the " +
                    "bots highest role.");
                return;
            }

            string reason = $"Unmuted by {Context.User} (ID: {Context.User.Id})";

            await Task.WhenAll(
                member.RemoveRoleAsync(role, new RequestOptions { AuditLogReason = reason }),
                _db.TryRemoveTemporaryMutedUserAsync(member)
            );

            await Task.WhenAll(
                _db.SaveChangesAsync(),
                ReplyAsync($"{member.Mention} has been unmuted.")
            );
        }

        //TODO add mutedmembers command

        [Group("mutedrole")]
        [Description("Configures a muted role.")]
        public class MutedRoleCommands : DiscordModuleBase
        {
            private readonly ModerationContext _db;

            public MutedRoleCommands(ModerationContext context) => _db = context;

            [Command("set", "")]
            [Description("Sets a role for muting members.")]
            [Remarks("This will set a role which will be used by the **mute** command.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task SetAsync(
                [Description("The role for muting members.")]
                [Remarks(
                    "This should optimally be a role which denies **Send Messages**, **Add Reactions**, **Speak** " +
                    "and **Video** permissions in all existing channels. You can use the **mutedrole create** " +
                    "command to automatically create a preconfigured muted role. However, there are no limitations " +
                    "on what role to use.")]
                [LowerHierarchy]
                [Remainder]
                IRole role)
            {
                //TODO handle users which have the old muted role assigned
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);

                if (config.RoleId == role.Id)
                {
                    await ReplyAsync($"Role for muting members is already set to {role.Mention}.",
                        allowedMentions: AllowedMentions.None);
                    return;
                }

                config.RoleId = role.Id;

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync($"Role for muting members has been set to {role.Mention}.")
                );
            }

            [Command("create", "generate")]
            [Description("Creates a preconfigured muted role.")]
            [Remarks(
                "This will create a role which denies **Send Messages**, **Add Reactions**, **Speak** and **Video** " +
                "permissions in all existing channels. The created role can be used with the **mutedrole set** " +
                "command. Keep in mind that when you create new channels, the permission overwrites for this role " +
                "won't be automatically updated. In those cases you can use the **mutedrole update** command to " +
                "automatically update the role.")]
            [RequireUserPermission(GuildPermission.ManageRoles | GuildPermission.SendMessages |
                GuildPermission.AddReactions | GuildPermission.Speak | GuildPermission.Stream)]
            [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.SendMessages |
                GuildPermission.AddReactions | GuildPermission.Speak | GuildPermission.Stream)]
            public async Task CreateAsync([Description("The name of the muted role."), Remainder] string name = "Muted")
            {
                var message = await ReplyAsync("Creating muted role. This may take a while …");
                IRole role;
                try
                {
                    role = await Context.Guild!.CreateRoleAsync(name, GuildPermissions.None,
                        isMentionable: false);

                    var user = (SocketGuildUser)Context.User;
                    var currentUser = Context.Guild.CurrentUser;

                    if (user.Hierarchy > currentUser.Hierarchy)
                        await role.ModifyAsync(x => x.Position = currentUser.Hierarchy);
                    else
                        await role.ModifyAsync(x => x.Position = user.Hierarchy);

                    var overwritePermissions = new OverwritePermissions(sendMessages: PermValue.Deny,
                        addReactions: PermValue.Deny, speak: PermValue.Deny, stream: PermValue.Deny);

                    foreach (var channel in Context.Guild.Channels)
                    {
                        var permissions = currentUser.GetPermissions(channel);
                        if (!permissions.ManageRoles || !permissions.ViewChannel)
                            continue;

                        await channel.AddPermissionOverwriteAsync(role, overwritePermissions);
                    }
                }
                catch
                {
                    await message.ModifyAsync(x => x.Content =
                        "An error occurred while creating the muted role. Please try again.");
                    return;
                }

                _db.AddPreconfiguredMutedRole(role);

                await Task.WhenAll(
                    message.ModifyAsync(x => x.Content = $"Muted role {role.Mention} has been created."),
                    _db.SaveChangesAsync()
                );
            }

            [Command("update")]
            [Description("Updates a preconfigured muted role.")]
            [Remarks(
                "This will reset all permissions of the role and denies **Send Messages**, **Add Reactions**, " +
                "**Speak** and **Video** permissions in all existing channels.")]
            [RequireUserPermission(GuildPermission.ManageRoles | GuildPermission.SendMessages |
                GuildPermission.AddReactions | GuildPermission.Speak | GuildPermission.Stream)]
            [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.SendMessages |
                GuildPermission.AddReactions | GuildPermission.Speak | GuildPermission.Stream)]
            public async Task UpdateAsync(
                [Description("The role to update")]
                [Remarks("This must be a role which has been created using the **mutedrole create** command.")]
                [Remainder]
                [LowerHierarchy]
                IRole role)
            {
                var mutedRole = await _db.GetPreconfiguredMutedRoleAsync(role);
                if (mutedRole is null)
                {
                    await ReplyAsync(
                        "You can only update roles which have been created using the **mutedrole create** command.");
                    return;
                }

                var message = await ReplyAsync($"Updating muted role {role.Mention}. This may take a while …",
                    allowedMentions: AllowedMentions.None);
                try
                {
                    var user = (SocketGuildUser)Context.User;
                    var currentUser = Context.Guild!.CurrentUser;

                    await role.ModifyAsync(x =>
                    {
                        x.Permissions = GuildPermissions.None;
                        x.Hoist = false;
                        x.Mentionable = false;

                        if (user.Hierarchy > currentUser.Hierarchy)
                            x.Position = Context.Guild.CurrentUser.Hierarchy;
                        else
                            x.Position = user.Hierarchy;
                    });

                    var overwritePermissions = new OverwritePermissions(sendMessages: PermValue.Deny,
                        addReactions: PermValue.Deny, speak: PermValue.Deny, stream: PermValue.Deny);

                    foreach (var channel in Context.Guild.Channels)
                    {
                        var permissions = currentUser.GetPermissions(channel);
                        if (!permissions.ManageRoles || !permissions.ViewChannel)
                            continue;

                        await channel.AddPermissionOverwriteAsync(role, overwritePermissions);
                    }
                }
                catch
                {
                    await message.ModifyAsync(x => x.Content =
                        "An error occurred while creating the muted role. Please try again.");
                    return;
                }

                await message.ModifyAsync(x => x.Content = $"Muted role {role.Mention} has been updated.");
            }

            [Command("settings")]
            [Description("Shows the current settings for the muted role.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.EmbedLinks)]
            public async Task SettingsAsync()
            {
                var config = await _db.GetGuildConfigurationAsync(Context.Guild!);

                var mutedRole = config?.RoleId is not null ? Context.Guild.GetRole(config.RoleId.Value) : null;
                string mutedRoleText = mutedRole?.Mention ?? "None";

                var embed = new SettingsEmbedBuilder(Context)
                    .WithTitle("Muted Role Settings")
                    .WithGuild(Context.Guild!)
                    .AddSetting("Muted Role", mutedRoleText, "set")
                    .Build();

                await ReplyAsync(embed: embed);
            }
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
