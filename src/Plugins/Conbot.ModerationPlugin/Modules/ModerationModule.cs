using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Conbot.Commands;
using Conbot.Extensions;

using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;

using Humanizer;

using Qmmands;

namespace Conbot.ModerationPlugin;

[Name("Moderation")]
[Description("Moderation related commands.")]
public class ModerationModule : ConbotGuildModuleBase
{
    private readonly ModerationContext _db;

    public ModerationModule(ModerationContext context) => _db = context;

    [Command("prune", "purge", "clean", "clear")]
    [Description("Deletes messages in a channel.")]
    [Remarks("Only up to 100 of the latest 1.000 messages in the executing channel will be deleted.")]
    [Commands.RequireAuthorChannelPermissions(Permission.ManageMessages)]
    [Commands.RequireBotChannelPermissions(Permission.ManageMessages)]
    public async Task<DiscordCommandResult> PruneAsync(
        [Description("The member to delete messages from.")]
        IMember? member = null,
        [Description("The maximal amount of messages to delete."), MinValue(1), MaxValue(100)]
        int limit = 10)
    {
        int count = await DeleteMessagesAsync(msg => member is null || msg.Author.Id == member.Id, limit);

        var text = new StringBuilder()
            .Append("messages".ToQuantity(count, "**#**"));

        if (member is not null)
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

        return Reply(text.ToString()).DeleteAfter(TimeSpan.FromSeconds(10));
    }

    public async ValueTask<int> DeleteMessagesAsync(Func<IMessage, bool> condition, int limit = 100)
    {
        var channelId = Context.ChannelId;

        var messageIds = new List<Snowflake>();
        int count = 0;
        var tasks = new List<Task>();
        var now = DateTimeOffset.UtcNow;
        var minimum = Snowflake.FromDateTimeOffset(now.Subtract(new TimeSpan(13, 23, 59, 0)));

        await foreach (var page in Context.Bot.EnumerateMessages(channelId, 1000))
        {
            foreach (var message in page)
            {
                if (count >= limit || message.Id <= minimum)
                    break;

                if (!condition(message))
                    continue;

                messageIds.Add(message.Id);
                count++;

                if (count % 100 != 0)
                    continue;

                tasks.Add(Bot.DeleteMessagesAsync(channelId, messageIds));
                messageIds = new List<Snowflake>();
            }
        }

        tasks.Add(Bot.DeleteMessagesAsync(channelId, messageIds));
        await Task.WhenAll(tasks);

        return count;
    }

    [Command("mute")]
    [Description("Mutes a member.")]
    [Remarks(
        "This will assign a \"muted role\" to the member. To use this command, the server must have a muted role " +
        "configured using the **mutedrole set** command.")]
    [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
    [Commands.RequireBotGuildPermissions(Permission.ManageRoles)]
    [Commands.RequireBotChannelPermissions(Permission.AddReactions | Permission.UseExternalEmojis)]
    public async Task<DiscordCommandResult> MuteAsync(
        [Description("The member to mute.")] IMember member,
        [Description("The duration for how long the member will be muted.")]
        [Remarks("If left blank, the member will be muted until someone manually unmutes them.")]
        [Remainder]
        TimeSpan? duration = null)
    {
        var config = await _db.GetGuildConfigurationAsync(Context.GuildId);

        IRole? role;

        if (config?.RoleId is null || (role = Bot.GetRole(Context.GuildId, config.RoleId.Value)) is null)
        {
            return Fail(
                "There is no role for muting members configured. Please configure a muted role with the " +
                "**mutedrole** command.");
        }

        if (member.Id == Context.Author.Id)
            return Fail("You can't mute yourself.");

        if (member.Id == Context.CurrentMember.Id)
            return Fail("You can't mute the bot.");

        if (role.Position >= Context.Author.GetHierarchy())
            return Fail("Muted role must be lower than your highest role.");

        if (role.Position >= Context.CurrentMember.GetHierarchy())
            return Fail("Muted role must be lower than the bot's highest role.");

        IUserMessage? message = null;

        if (member.RoleIds.Contains(config.RoleId.Value))
        {
            var prompt = Prompt("This member is already muted. Do you want to adjust the duration?");
            message = await prompt;

            if (prompt.Result != true)
                return Modify(message, "Duration for the mute hasn't been adjusted.");
        }

        string text;

        if (duration is null)
        {
            string reason = $"Muted by {Context.Author} (ID: {Context.Author.Id})";

            await Task.WhenAll(
                member.GrantRoleAsync(role.Id, new DefaultRestRequestOptions { Reason = reason }),
                _db.TryRemoveTemporaryMutedUserAsync(member)
            );

            text = $"{member.Mention} is now muted.";
        }
        else
        {
            string reason =
                $"Temporarily muted for {duration.Value.ToLongString()} by {Context.Author} (ID: {Context.Author.Id})";

            var now = DateTime.UtcNow;

            await Task.WhenAll(
                member.GrantRoleAsync(role.Id, new DefaultRestRequestOptions { Reason = reason }),
                _db.CreateOrUpdateTemporaryMutedUserAsync(member, role, now, now.Add(duration.Value))
            );

            text = $"{member.Mention} is now temporarily muted for {duration.Value.ToLongFormattedString()}.";
        }

        DiscordCommandResult result;

        if (message is null)
            result = Reply(text);
        else
            result = Modify(message, text);

        return result.RunWith(_db.SaveChangesAsync());
    }

    [Command("unmute")]
    [Description("Unmutes a muted member.")]
    [Remarks(
        "You can also unmute members by manually removing the muted role from them. However, there might be " +
        "cases where the bot doesn't detect the removal of a role (e.g. when the bot was offline at that time). " +
        "In those cases the bot might try to remove the role from the member after the mute duration expired " +
        "when the member was temporarily muted. So unmuting a member using this command is recommended. You can " +
        "use the **mutedmembers** command to check which members are currently temporarily muted.")]
    public async Task<DiscordCommandResult> UnmuteAsync([Description("The member to unmute.")] IMember member)
    {
        var config = await _db.GetGuildConfigurationAsync(Context.GuildId);

        IRole? role;
        if (config?.RoleId is null || (role = Bot.GetRole(Context.GuildId, config.RoleId.Value)) is null)
        {
            return Fail(
                "There is no role for muting members configured. Please configure a muted role with the " +
                "**mutedrole** command.");
        }

        if (!member.RoleIds.Contains(config.RoleId.Value))
            return Fail("This member isn't muted.");

        if (role.Position >= Context.Author.GetHierarchy())
            return Fail("Muted role must be lower than your highest role.");

        if (role.Position >= Context.CurrentMember.GetHierarchy())
            return Fail("Muted role must be lower than the bot's highest role.");

        string reason = $"Unmuted by {Context.Author} (ID: {Context.Author.Id})";

        await Task.WhenAll(
            member.RevokeRoleAsync(role.Id, new DefaultRestRequestOptions { Reason = reason }),
            _db.TryRemoveTemporaryMutedUserAsync(member)
        );

        return Reply($"{member.Mention} has been unmuted.").RunWith(_db.SaveChangesAsync());
    }

    //TODO add mutedmembers command

    [Group("mutedrole")]
    [Description("Configures a muted role.")]
    public class MutedRoleCommands : ConbotGuildModuleBase
    {
        private readonly ModerationContext _db;

        public MutedRoleCommands(ModerationContext context) => _db = context;

        [Command("set", "")]
        [Description("Sets a role for muting members.")]
        [Remarks("This will set a role which will be used by the **mute** command.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
        public async Task<DiscordCommandResult> SetAsync(
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
            var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild);

            if (config.RoleId == role.Id)
                return Fail($"Role for muting members is already set to {role.Mention}.");

            config.RoleId = role.Id;

            return Reply($"Role for muting members has been set to {role.Mention}.")
                .RunWith(_db.SaveChangesAsync());
        }

        [Command("create", "generate")]
        [Description("Creates a preconfigured muted role.")]
        [Remarks(
            "This will create a role which denies **Send Messages**, **Add Reactions**, **Speak** and **Video** " +
            "permissions in all existing channels. The created role can be used with the **mutedrole set** " +
            "command. Keep in mind that when you create new channels, the permission overwrites for this role " +
            "won't be automatically updated. In those cases you can use the **mutedrole update** command to " +
            "automatically update the role.")]
        [Commands.RequireAuthorGuildPermissions(
            Permission.ManageRoles | Permission.SendMessages | Permission.AddReactions |
            Permission.Speak | Permission.Stream)]
        [Commands.RequireBotGuildPermissions(
            Permission.ManageRoles | Permission.SendMessages | Permission.AddReactions |
            Permission.Speak | Permission.Stream)]
        public async Task<DiscordCommandResult> CreateAsync(
            [Description("The name of the muted role."), Remainder]
            string name = "Muted")
        {
            var message = await Reply("Creating muted role. This may take a while …");

            IRole role;
            try
            {
                role = await Context.Guild.CreateRoleAsync(x =>
                {
                    x.Name = name;
                    x.Permissions = GuildPermissions.None;
                    x.IsMentionable = false;
                });

                var author = Context.Author;
                var currentMember = Context.CurrentMember;

                if (author.GetHierarchy() > currentMember.GetHierarchy())
                    await role.ModifyAsync(x => x.Position = currentMember.GetHierarchy());
                else
                    await role.ModifyAsync(x => x.Position = author.GetHierarchy());

                var overwritePermissions = new OverwritePermissions()
                    .Deny(Permission.SendMessages)
                    .Deny(Permission.AddReactions)
                    .Deny(Permission.Speak)
                    .Deny(Permission.Stream);

                var overwrite = new LocalOverwrite(role.Id, OverwriteTargetType.Role, overwritePermissions);

                foreach (var (_, channel) in Context.Guild.GetChannels())
                {
                    var permissions = currentMember.GetPermissions(channel);

                    if (!permissions.ManageRoles || !permissions.ViewChannels)
                        continue;

                    await channel.SetOverwriteAsync(overwrite);
                }
            }
            catch
            {
                return Fail("An error occurred while creating the muted role. Please try again.");
            }

            _db.AddPreconfiguredMutedRole(role);

            return Modify(message, $"Muted role {role.Mention} has been created.").RunWith(_db.SaveChangesAsync());
        }

        [Command("update")]
        [Description("Updates a preconfigured muted role.")]
        [Remarks(
            "This will reset all permissions of the role and denies **Send Messages**, **Add Reactions**, " +
            "**Speak** and **Video** permissions in all existing channels.")]
        [Commands.RequireAuthorGuildPermissions(
            Permission.ManageRoles | Permission.SendMessages | Permission.AddReactions |
            Permission.Speak | Permission.Stream)]
        [Commands.RequireBotGuildPermissions(
            Permission.ManageRoles | Permission.SendMessages | Permission.AddReactions |
            Permission.Speak | Permission.Stream)]
        public async Task<DiscordCommandResult> UpdateAsync(
            [Description("The role to update")]
            [Remarks("This must be a role which has been created using the **mutedrole create** command.")]
            [Remainder]
            [LowerHierarchy]
            IRole role)
        {
            var mutedRole = await _db.GetPreconfiguredMutedRoleAsync(role);

            if (mutedRole is null)
            {
                return Fail(
                    "You can only update roles which have been created using the **mutedrole create** command.");
            }

            var message = await Reply($"Updating muted role {role.Mention}. This may take a while …");

            try
            {
                var author = Context.Author;
                var currentMember = Context.CurrentMember;

                await role.ModifyAsync(x =>
                {
                    x.Permissions = GuildPermissions.None;
                    x.IsHoisted = false;
                    x.IsMentionable = false;

                    if (author.GetHierarchy() > currentMember.GetHierarchy())
                        x.Position = currentMember.GetHierarchy();
                    else
                        x.Position = author.GetHierarchy();
                });

                var overwritePermissions = new OverwritePermissions()
                    .Deny(Permission.SendMessages)
                    .Deny(Permission.AddReactions)
                    .Deny(Permission.Speak)
                    .Deny(Permission.Stream);

                var overwrite = new LocalOverwrite(role.Id, OverwriteTargetType.Role, overwritePermissions);

                foreach (var (_, channel) in Context.Guild.GetChannels())
                {
                    var permissions = currentMember.GetPermissions(channel);

                    if (!permissions.ManageRoles || !permissions.ViewChannels)
                        continue;

                    await channel.SetOverwriteAsync(overwrite);
                }
            }
            catch
            {
                return Fail("An error occurred while creating the muted role. Please try again.");
            }

            return Modify(message, $"Muted role {role.Mention} has been updated.");
        }

        [Command("settings")]
        [Description("Shows the current settings for the muted role.")]
        [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
        [Commands.RequireBotChannelPermissions(Permission.SendEmbeds)]
        public async Task<DiscordCommandResult> SettingsAsync()
        {
            var config = await _db.GetGuildConfigurationAsync(Context.Guild);

            var mutedRole = config?.RoleId is not null
                ? Context.Guild.Roles.Values.FirstOrDefault(x => x.Id == config.RoleId.Value)
                : null;

            string mutedRoleText = mutedRole?.Mention ?? "None";

            var embed = new SettingsEmbedBuilder(Context)
                .WithTitle("Muted Role Settings")
                .WithGuild(Context.Guild)
                .AddSetting("Muted Role", mutedRoleText, "set")
                .Build();

            return Reply(embed);
        }
    }

    [Command("ban")]
    [Description("Bans a member.")]
    [Commands.RequireAuthorGuildPermissions(Permission.BanMembers)]
    [Commands.RequireBotGuildPermissions(Permission.BanMembers)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> BanAsync(
        [Description("The member to ban.")]
        [LowerHierarchy]
        IMember member,
        [Name("prune days")]
        [Description("The amount of days to prune messages from the member.")]
        [MinValue(0), MaxValue(7)]
        int pruneDays = 0,
        [Description("The reason for the ban.")]
        [Remarks("The reason will show up in the audit log.")]
        [Remainder]
        string? reason = null)
    {
        if (member.Id == Context.Author.Id)
            return Fail("You can't ban yourself.");

        await Context.Guild.CreateBanAsync(member.Id, reason, pruneDays);
        return Reply($"**{Markdown.Escape(member.ToString())}** has been banned.");
    }

    [Command("hackban")]
    [Description("Bans a user by ID.")]
    [Remarks("This is useful to ban a user that isn't in the server.")]
    [Commands.RequireAuthorGuildPermissions(Permission.BanMembers)]
    [Commands.RequireBotGuildPermissions(Permission.BanMembers)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> HackbanAsync(
        [Description("The ID of the user to ban.")]
        [Snowflake(SnowflakeType.User)]
        ulong id,
        [Name("prune days")]
        [Description("The amount of days to prune messages from the user.")]
        [MinValue(0)]
        [MaxValue(7)]
        int pruneDays = 0,
        [Description("The reason for the ban.")]
        [Remarks("The reason will show up in the audit log.")]
        [Remainder]
        string? reason = null)
    {
        if (id == Context.Author.Id)
            return Fail("You can't ban yourself.");

        try
        {
            await Context.Guild.CreateBanAsync(id, reason, pruneDays);
        }
        catch (RestApiException e)
        {
            if (e.StatusCode == HttpResponseStatusCode.NotFound)
                return Fail("User hasn't been found.");

            return Fail("The bot isn't able to ban this user.");
        }

        return Reply($"User with ID **{id}** has been banned.");
    }

    [Command("unban")]
    [Description("Revokes a ban from a user.")]
    [Commands.RequireAuthorGuildPermissions(Permission.BanMembers)]
    [Commands.RequireBotGuildPermissions(Permission.BanMembers)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> UnbanAsync(
        [Description("The user to revoke the ban from.")]
        IMember user)
    {
        try
        {
            await Context.Guild.DeleteBanAsync(user.Id);
        }
        catch (RestApiException)
        {
            return Fail("User hasn't been found in the ban list.");
        }

        return Reply($"**{Markdown.Escape(user.ToString())}** has been unbanned.");
    }

    [Command("softban")]
    [Description("Soft bans a member.")]
    [Remarks(
        "A soft ban is like a kick but instead kicking the member, " +
        "the member will be banned and directly unbanned. " +
        "This is useful for pruning messages from the member.")]
    [Commands.RequireAuthorGuildPermissions(Permission.BanMembers, Permission.KickMembers)]
    [Commands.RequireBotGuildPermissions(Permission.BanMembers)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> SoftBanAsync(
        [Description("The member to soft ban.")]
        [LowerHierarchy]
        IMember member,
        [Name("prune days")]
        [Description("The amount of days to prune messages from the member.")]
        [MinValue(0), MaxValue(7)]
        int pruneDays = 0,
        [Description("The reason for the soft ban.")]
        [Remarks("The reason will show up in the audit log.")]
        [Remainder]
        string? reason = null)
    {
        if (member.Id == Context.Author.Id)
            return Fail("You can't soft ban yourself.");

        await Context.Guild.CreateBanAsync(member.Id, reason, pruneDays);
        await Context.Guild.DeleteBanAsync(member.Id);

        return Reply($"**{Markdown.Escape(member.ToString())}** has been soft banned.");
    }

    [Command("kick")]
    [Description("Kicks a member.")]
    [Commands.RequireAuthorGuildPermissions(Permission.BanMembers, Permission.KickMembers)]
    [Commands.RequireBotGuildPermissions(Permission.KickMembers)]
    [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
    public async Task<DiscordCommandResult> KickAsync(
        [Description("The member to kick.")]
        [LowerHierarchy]
        IMember member,
        [Description("The reason for the kick.")]
        [Remarks("The reason will show up in the audit log.")]
        [Remainder]
        string? reason = null)
    {
        if (member.Id == Context.Author.Id)
            return Fail("You can't kick yourself.");

        await member.KickAsync(new DefaultRestRequestOptions { Reason = reason });

        return Reply($"**{Markdown.Escape(member.ToString())}** has been kicked.");
    }
}