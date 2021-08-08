using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Interactive;

using Disqord;
using Disqord.Bot;
using Disqord.Gateway;

using Humanizer;

using Qmmands;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Conbot.RankingPlugin
{
    [Name("Ranking")]
    [Group("rank", "level")]
    [Description("Gain experience points by writing messages in a server.")]
    [Remarks(
        "Each time you write a message, you randomly gain 5-15 experience points. However, to avoid gaining " +
        "experience points by spamming messages, a one minute cooldown is in effect each time after you gained " +
        "experience points.\n\n" +
        "With gaining experience points, you increase your level. The higher your level is, the more experience " +
        "points you need to achieve a level up. The formula to determine the total amount of experience points " +
        "needed for a specific level is `(n^2.5) * 10` where n is the level.")]
    public class RankingModule : ConbotGuildModuleBase
    {
        private readonly RankingContext _db;
        private readonly RankingService _rankingService;
        private readonly IConfiguration _config;

        public RankingModule(RankingContext context, RankingService service, IConfiguration config)
        {
            _db = context;
            _rankingService = service;
            _config = config;
        }

        [Command("show", "")]
        [Description("Shows your or someone else's rank in the server.")]
        [Commands.RequireBotChannelPermissions(Permission.SendEmbeds)]
        public async Task<DiscordCommandResult> RankAsync(
            [Description("The member to show the rank of.")] IMember? member = null)
        {
            member ??= Context.Author;

            Rank? foundRank = null;
            int index = 0;
            int total = 0;

            await foreach (var rank in _db.GetRanksAsync(Context.Guild!, true))
            {
                if (rank.UserId == member.Id)
                {
                    foundRank = rank;
                    index = total;
                }

                if (!rank.IsBot)
                    total++;
            }

            if (foundRank is null)
            {
                if (member.Id == Context.Author.Id)
                    return Reply("You don't have a rank on this server yet.");
                else
                    return Reply("This member has no rank on this server.");
            }

            var embed = CreateRankEmbed(member, foundRank.ExperiencePoints, foundRank.RankedMessages, index + 1, total);
            return Reply(embed);
        }

        public LocalEmbed CreateRankEmbed(IMember user, int experiencePoints, int messages, int rank, int total)
        {
            int level = _rankingService.GetLevel(experiencePoints);
            int levelExperiencePoints = experiencePoints - _rankingService.GetTotalExperiencePoints(level);
            int nextLevelExperiencePoints = _rankingService.GetNextLevelExperiencePoints(level);
            double percentage = (double)levelExperiencePoints / nextLevelExperiencePoints;

            var descriptionText = new StringBuilder();

            if (!user.IsBot)
            {
                if (rank == 1)
                    descriptionText.Append("🥇 ");
                else if (rank == 2)
                    descriptionText.Append("🥈 ");
                else if (rank == 3)
                    descriptionText.Append("🥉 ");
            }

            descriptionText
                .Append("Level ")
                .Append(Markdown.Bold(level.ToString()))
                .Append(" (")
                .Append(percentage.ToString("P2", CultureInfo.InvariantCulture))
                .Append(')');

            var embed = new LocalEmbed()
                .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithDescription(descriptionText.ToString());

            if (!user.IsBot)
                embed.AddField("Rank", $"{rank:n0}/{total:n0}", true);
            else
                embed.WithFooter("Bots will gain XP but don't count towards the ranking.");

            embed
                .AddField("Next Level", $"{levelExperiencePoints:n0}/{nextLevelExperiencePoints:n0} XP", true)
                .AddField("Total XP", $"{experiencePoints:n0} XP", true)
                .AddField("Messages", $"{messages:n0}", true);

            return embed;
        }

        [Command("leaderboard", "list", "all")]
        [Description("Shows the leaderboard of the server.")]
        [Commands.RequireBotChannelPermissions(
            Permission.AddReactions |
            Permission.SendEmbeds |
            Permission.UseExternalEmojis)]
        public async Task<DiscordCommandResult> ListAsync([Description("The page to start with.")] int page = 1)
        {
            var ranks = await _db.GetRanksAsync(Context.Guild!).ToArrayAsync();

            var text = new StringBuilder();
            var pageDescriptions = new List<string>();

            int padding = ranks.Length.ToString().Length;

            for (int i = 0; i < ranks.Length; i++)
            {
                var rank = ranks[i];

                var user = Bot.GetMember(Context.GuildId!, rank.UserId);

                if (user is null)
                    continue;

                int level = _rankingService.GetLevel(rank.ExperiencePoints);
                int levelExperiencePoints = rank.ExperiencePoints - _rankingService.GetTotalExperiencePoints(level);
                int nextLevelExperiencePoints = _rankingService.GetNextLevelExperiencePoints(level);
                double percentage = (double)levelExperiencePoints / nextLevelExperiencePoints;

                text.Append('`')
                    .Append((i + 1).ToString().PadLeft(padding))
                    .Append(".`")
                    .Append(' ')
                    .AppendLine(user.Mention)
                    .Append("> ");

                if (i == 0)
                    text.Append("🥇 ");
                else if (i == 1)
                    text.Append("🥈 ");
                else if (i == 2)
                    text.Append("🥉 ");

                text
                    .Append("Level **")
                    .Append(level)
                    .Append("** (")
                    .Append(percentage.ToString("P2", CultureInfo.InvariantCulture))
                    .AppendLine(")")
                    .Append("> ")
                    .Append(levelExperiencePoints.ToString("n0", CultureInfo.InvariantCulture))
                    .Append('/')
                    .Append(nextLevelExperiencePoints.ToString("n0", CultureInfo.InvariantCulture))
                    .Append(" XP │ ")
                    .Append(rank.ExperiencePoints.ToString("n0", CultureInfo.InvariantCulture))
                    .AppendLine(" XP Total");

                if ((i + 1) % 5 == 0 || i == ranks.Length - 1)
                {
                    pageDescriptions.Add(text.ToString());
                    text.Clear();
                }
            }

            List<Page> pages = new();

            for (int i = 0; i < pageDescriptions.Count; i++)
            {
                var embed = new LocalEmbed()
                    .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                    .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                    .WithTitle("Leaderboard")
                    .WithDescription(pageDescriptions[i])
                    .WithFooter($"Page {i + 1}/{pageDescriptions.Count} ({"entry".ToQuantity(ranks.Length)})");

                pages.Add(new Page().WithEmbeds(embed));
            }

            return Paginate(pages, startIndex: page - 1);
        }

        [Group("announcements")]
        [Description("Configures level up announcements.")]
        public class Announcements : ConbotModuleBase
        {
            private readonly RankingContext _db;

            public Announcements(RankingContext context)
            {
                _db = context;
            }

            [Command("toggle", "")]
            [Description("Toggles level up announcements.")]
            [Commands.RequireGuild]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
            public async Task<DiscordCommandResult> ToggleAsync(
                [Description("Wether to enable or disable level up announcements.")]
                [Choices("enable", "disable")]
                string toggle)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.GuildId!.Value);

                string? text = null;

                switch (toggle)
                {
                    case "enable":
                        if (config.ShowLevelUpAnnouncements == true)
                            return Fail("Level up announcements are already enabled.");

                        config.ShowLevelUpAnnouncements = true;
                        text = "Level up announcements have been enabled.";

                        break;
                    case "disable":
                        if (config.ShowLevelUpAnnouncements == false)
                            return Fail("Level up announcements are already disabled.");

                        config.ShowLevelUpAnnouncements = false;
                        text = "Level up announcements have been disabled.";

                        break;
                }

                return Reply(text).RunWith(_db.SaveChangesAsync());
            }

            [Command("channel")]
            [Description("Sets a channel for level up announcements.")]
            [Commands.RequireGuild]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
            public async Task<DiscordCommandResult> ChannelAsync(
                [Description("The text channel for level up announcements.")]
                [Remarks(
                    "If you leave this blank, then the channel will be set to **Current Channel** which will send " +
                    "level up announcements to the channel where the member achieved the level up.")]
                ITextChannel? channel = null)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.GuildId!.Value);
                config.LevelUpAnnouncementsChannelId = channel?.Id;

                string text;

                if (channel is not null)
                    text = $"Channel for level up announcements has been set to {channel.Mention}.";
                else
                    text = "Channel for level up announcements has been set to **Current Channel**.";

                return Reply(text).RunWith(_db.SaveChangesAsync());
            }

            [Command("minimumlevel", "minlevel")]
            [Description("Sets a minimum level for level up announcements.")]
            [Remarks("This is useful to avoid spamming with lower levels where it's faster to achieve a level up.")]
            [Commands.RequireGuild]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
            public async Task<DiscordCommandResult> MinimumLevelAsync(
                [Description("The minimum level.")]
                [Remarks(
                    "If you leave this blank, then the minimum level will set to **None** which will send level up " +
                    "announcements for all levels.")]
                [MinValue(1), MaxValue(1000)]
                int? level = null)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.GuildId!.Value);
                config.LevelUpAnnouncementsMinimumLevel = level;

                string text = level is not null
                    ? $"Minimum level for level up announcements has been set to **{level}**."
                    : "Minimum level for level up announcements has been set to **None**.";

                return Reply(text).RunWith(_db.SaveChangesAsync());
            }

            [Command("settings")]
            [Description("Shows the current settings for level up announcements.")]
            [Commands.RequireBotChannelPermissions(Permission.SendEmbeds)]
            public async Task<DiscordCommandResult> SettingsAsync(
                [Description("Wether you want to show the server's or your personal settings.")]
                [Remarks("You can only view the server settings if you have the **Manage Server** permission.")]
                [Choices("server", "user")]
                string type = "server")
            {
                LocalEmbed embed;

                if (type == "server")
                {
                    if (Context.Author is not IMember member)
                        return Fail("You can only view the server's settings for level up announcements on a server.");

                    if (!member.GetPermissions().Has(Permission.ManageGuild))
                    {
                        return Fail("You require the **Manage Server** permission to view the server's settings for " +
                            "level up announcements.");
                    }

                    var config = await _db.GetGuildConfigurationAsync(Context.GuildId!.Value);

                    bool enabled = config?.ShowLevelUpAnnouncements ?? false;
                    string channel = config?.LevelUpAnnouncementsChannelId is not null
                        ? Mention.Channel(config.LevelUpAnnouncementsChannelId.Value)
                        : "Current Channel";
                    string minimumlevel = config?.LevelUpAnnouncementsMinimumLevel is not null
                        ? config.LevelUpAnnouncementsMinimumLevel.ToString()!
                        : "None";

                    embed = new SettingsEmbedBuilder(Context)
                        .WithTitle("Level Up Announcements Server Settings")
                        .WithGuild(Bot.GetGuild(Context.GuildId!.Value))
                        .AddSetting("Show Level Up Announcements", enabled, "toggle")
                        .AddSetting("Announcements Channel", channel, "channel")
                        .AddSetting("Minimum Level", minimumlevel, "minimumlevel")
                        .Build();
                }
                else
                {
                    var config = await _db.GetUserConfigurationAsync(Context.Author);

                    bool allowMentions = config?.AnnouncementsAllowMentions ?? false;
                    bool directMessages = config?.AnnouncementsSendDirectMessages ?? false;

                    embed = new SettingsEmbedBuilder(Context)
                        .WithTitle("Level Up Announcements User Settings")
                        .WithUser(Context.Author)
                        .AddSetting("Mention Notifications", allowMentions, "mentions")
                        .AddSetting("Send Direct Messages", directMessages, "directmessages")
                        .Build();
                }

                return Reply(embed);
            }

            [Command("mentions", "notifications")]
            [Description("Toggles notifications for mentions in level up announcements.")]
            [Remarks(
                "This setting is per user and global. If you enable this setting, you'll receive a notification when " +
                "you get mentioned in level up announcements. Disabling this setting will still send level up " +
                "announcements if the server has them enabled; however, you won't get a notification that you got " +
                "mentioned.")]
            public async Task<DiscordCommandResult> MentionAsync(
                [Description("Wether to enable or disable notifications for mentions in level up announcements.")]
                [Choices("enable", "disable")]
                string toggle)
            {
                var config = await _db.GetOrCreateUserConfigurationAsync(Context.Author);

                string? text = null;

                switch (toggle)
                {
                    case "enable":
                        if (config.AnnouncementsAllowMentions ?? false)
                        {
                            return Fail(
                                "You already receive notifications when you get mentioned in level up announcements.");
                        }

                        config.AnnouncementsAllowMentions = true;
                        text =
                            "You'll now receive notifications when you get mentioned in level up announcements.";

                        break;
                    case "disable":
                        if (!(config.AnnouncementsAllowMentions ?? false))
                        {
                            return Fail("You already don't receive notifications when you get mentioned in level up " +
                                "announcements.");
                        }

                        config.AnnouncementsAllowMentions = false;
                        text =
                            "You'll no longer receive notifications when you get mentioned in level up announcements.";

                        break;
                }

                return Reply(text).RunWith(_db.SaveChangesAsync());
            }

            [Command("directmessages")]
            [Description("Toggles direct messages for level up announcements.")]
            [Remarks(
                "This setting is per user and global. If you enable this setting, you'll receive a direct message " +
                "whenever you achieved a level up on any server. However, it will only send a direct message if " +
                "level up announcements are *not* enabled on the server or you don't meet the minimum level " +
                "specified by the server. If a server has level up announcements enabled and you meet the minimum " +
                "level, then they will be sent on the server instead.")]
            public async Task<DiscordCommandResult> DirectMessagesAsync(
                [Description("Wether to enable or disable direct messages for level up announcements.")]
                [Choices("enable", "disable")]
                string toggle)
            {
                var config = await _db.GetOrCreateUserConfigurationAsync(Context.Author);

                string? text = null;

                switch (toggle)
                {
                    case "enable":
                        if (config.AnnouncementsSendDirectMessages ?? false)
                            return Fail("You already receive direct messages for level up announcements.");

                        config.AnnouncementsSendDirectMessages = true;
                        text =
                            "You'll now receive direct messages for level up announcements if a server has them disabled.";

                        break;
                    case "disable":
                        if (!(config.AnnouncementsSendDirectMessages ?? false))
                            return Fail("You already don't receive direct messages for level up announcements.");

                        config.AnnouncementsSendDirectMessages = false;
                        text =
                            "You'll no longer receive direct messages for level up announcements.";

                        break;
                }

                return Reply(text).RunWith(_db.SaveChangesAsync());
            }
        }

        [Group("rolerewards", "role", "rewards")]
        [Description("Configures automated role rewards.")]
        [Remarks(
            "You can set roles which will be automatically given to a member when they reach a specific " +
            "level. Roles of a member will only update after they gained XP.")]
        public class RoleRewardsCommands : ConbotGuildModuleBase
        {
            private readonly RankingContext _db;
            private readonly RankingService _rankingService;
            private readonly IConfiguration _config;

            public RoleRewardsCommands(RankingContext context, RankingService rankingService, IConfiguration config)
            {
                _db = context;
                _rankingService = rankingService;
                _config = config;
            }

            [Command("list", "all", "")]
            [Description("Lists all available role rewards.")]
            [Commands.RequireBotChannelPermissions(
                Permission.AddReactions |
                Permission.SendEmbeds |
                Permission.UseExternalEmojis)]
            public async Task<DiscordCommandResult> ListAsync([Description("The page to start with.")] int page = 1)
            {
                var roleRewards = await _db.GetRoleRewardsAsync(Context.Guild!).ToListAsync();
                int count = roleRewards.Count;

                if (count == 0)
                    return Reply("There are no rewards available for this server.");

                int currentPage = 1;
                int totalPages = (count / 10) + (count % 10 != 0 ? 1 : 0);

                List<Page> pages = new();
                var embed = new LocalEmbed();

                for (int i = 0; i < count; i++)
                {
                    var roleReward = roleRewards[i];
                    int level = roleReward.Level;
                    int experiencePoints = _rankingService.GetTotalExperiencePoints(level);

                    embed.AddField(
                        $"Level {level} ({experiencePoints.ToString("n0", CultureInfo.CurrentCulture)} XP)",
                        Mention.Role(roleReward.RoleId));

                    if ((i + 1) % 10 == 0 || i == count - 1)
                    {
                        embed
                            .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                            .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                            .WithTitle("Role Rewards")
                            .WithFooter($"Page {currentPage}/{totalPages} ({"entry".ToQuantity(count)})");

                        currentPage++;
                        pages.Add(new Page().WithEmbeds(embed));
                        embed = new LocalEmbed();
                    }
                }

                return Paginate(pages, startIndex: page - 1);
            }

            [Command("add")]
            [Description("Adds a role reward for a specific level.")]
            [Remarks(
                "Only one role can be rewarded for a specific level and only one level can be set for a specific role.")]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
            [Commands.RequireBotGuildPermissions(Permission.ManageRoles)]
            public async Task<DiscordCommandResult> AddAsync(
                [Description("The role to reward.")]
                [Remarks(
                    "Make sure the role is *below* the bots highest role. Otherwise the bot won't be able " +
                    "to give this role to members.")]
                [LowerHierarchy]
                [Assignable]
                IRole role,
                [Description("The level when the role should be rewarded.")]
                int level)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);

                var roleReward = await _db.GetRoleRewardAsync(Context.Guild, level);
                if (roleReward is not null)
                    return Fail("There already exists a reward for this level.");

                roleReward = await _db.GetRoleRewardAsync(role);
                if (roleReward is not null)
                    return Fail("There already exists a reward for this role.");

                await _db.AddRoleRewardAsync(config, level, role);

                return Reply($"Role {role.Mention} has been added as a reward for level **{level}**.")
                    .RunWith(_db.SaveChangesAsync());
            }

            [Command("remove")]
            [Description("Removes a role reward.")]
            [Remarks("This will automatically remove the role from everyone.")]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
            [Commands.RequireBotGuildPermissions(Permission.ManageRoles)]
            public async Task<DiscordCommandResult> RemoveAsync(
                [Description("The role of the reward to remove."), Remainder] IRole role)
            {
                var roleReward = await _db.GetRoleRewardAsync(role);

                if (roleReward is null)
                    return Fail("There exists no reward for this role.");

                _db.RemoveRoleReward(roleReward);

                return Reply($"Role {role.Mention} has been removed as a reward for level **{roleReward.Level}**.")
                    .RunWith(_db.SaveChangesAsync());
            }

            [Command("type")]
            [Description("Sets the type for role rewards.")]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
            [Commands.RequireBotGuildPermissions(Permission.ManageRoles)]
            public async Task<DiscordCommandResult> StackAsync(
                [Choices("stack", "remove")]
                [Description("The type for role rewards.")]
                [Remarks(
                    "With the type set to **stack**, members will keep all previous rewards. " +
                    "With the type set to **remove**, members will only keep the highest reward. " +
                    "By default the type is set to **stack**.")]
                string type)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);
                string? text = null;

                switch (type)
                {
                    case "stack":
                        if (config.RoleRewardsType == RoleRewardsType.Stack)
                            return Fail("Type for role rewards is already set to **stack**.");

                        config.RoleRewardsType = RoleRewardsType.Stack;
                        text = "Type for role rewards has been set to **stack**.";
                        break;
                    case "remove":
                        if (config.RoleRewardsType == RoleRewardsType.Remove)
                            return Fail("Type for role rewards is already set to **remove**.");

                        config.RoleRewardsType = RoleRewardsType.Remove;
                        text = "Type for role rewards has been set to **remove**.";
                        break;
                }

                return Reply(text).RunWith(_db.SaveChangesAsync());
            }

            [Command("settings")]
            [Description("Shows the current settings for role rewards.")]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageRoles)]
            [Commands.RequireBotGuildPermissions(Permission.SendEmbeds)]
            public async Task<DiscordCommandResult> SettingsAsync()
            {
                var config = await _db.GetGuildConfigurationAsync(Context.Guild!);

                var type = config?.RoleRewardsType ?? RoleRewardsType.Stack;

                string typeText = type == RoleRewardsType.Stack
                    ? "Stack (members will keep all previous rewards)"
                    : "Remove (members will only keep the highest reward)";

                var embed = new SettingsEmbedBuilder(Context)
                    .WithTitle("Role Rewards Settings")
                    .WithGuild(Context.Guild!)
                    .AddSetting("Type", typeText, "type")
                    .Build();

                return Reply(embed);
            }
        }

        [Group("ignore")]
        [Description("Configures channels to ignore from gaining experience points.")]
        [Remarks(
            "Ignored channels are useful to avoid gaining experience points from channels where usually no " +
            "actual discussion is held, e.g., bot channels.")]
        public class IgnoredChannelsCommands : ConbotGuildModuleBase
        {
            private readonly RankingContext _db;
            private readonly IConfiguration _config;

            public IgnoredChannelsCommands(RankingContext context, IConfiguration config)
            {
                _db = context;
                _config = config;
            }

            [Command("add", "")]
            [Description("Adds a channel to ignore.")]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
            [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
            public async Task<DiscordCommandResult> AddAsync(
                [Description("The text channel to ignore.")] ITextChannel channel)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);

                var ignoredChannel = await _db.GetIgnoredChannelAsync(channel);
                if (ignoredChannel is not null)
                    return Fail("This channel is already ignored.");

                _db.AddIgnoredChannel(channel, config);

                return Reply($"Channel {channel.Mention} is now ignored from gaining experience points.")
                    .RunWith(_db.SaveChangesAsync());
            }

            [Command("remove")]
            [Description("Removes a channel from being ignored.")]
            [Commands.RequireAuthorGuildPermissions(Permission.ManageGuild)]
            [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
            public async Task<DiscordCommandResult> RemoveAsync(
                [Description("The text channel to remove from being ignored.")] ITextChannel channel)
            {
                var ignoredChannel = await _db.GetIgnoredChannelAsync(channel);

                if (ignoredChannel is null)
                    return Fail("This channel isn't ignored.");

                _db.RemoveIgnoredChannel(ignoredChannel);

                return Reply($"Channel {channel.Mention} is now no longer ignored from gaining experience points.")
                    .RunWith(_db.SaveChangesAsync());
            }

            [Command("list", "all")]
            [Description("Lists all ignored channels.")]
            [Commands.RequireBotChannelPermissions(
                Permission.AddReactions |
                Permission.SendEmbeds |
                Permission.UseExternalEmojis)]
            public async Task<DiscordCommandResult> ListAsync([Description("The page to start with.")] int page = 1)
            {
                var ignoredChannels = await _db.GetIgnoredChannelsAsync(Context.GuildId).ToArrayAsync();
                int count = ignoredChannels.Length;

                if (count == 0)
                    return Reply("There are no channels ignored from gaining experience points.");

                List<string> pageDescription = new();

                int i = 1;
                var pageText = new StringBuilder();

                foreach (var ignoredChannel in ignoredChannels)
                {
                    pageText.AppendLine(Mention.Channel(ignoredChannel.ChannelId));

                    if (i % 15 == 0 || i == count)
                    {
                        pageDescription.Add(pageText.ToString());
                        pageText.Clear();
                    }

                    i++;
                }

                if (page > pageDescription.Count || page < 1)
                    return Fail("This page doesn't exist.");

                List<Page> pages = new();

                for (int j = 0; j < pageDescription.Count; j++)
                {
                    var embed = new LocalEmbed()
                        .WithColor(new Color(_config.GetValue<int>("DefaultEmbedColor")))
                        .WithAuthor(Context.Guild!.Name, Context.Guild.GetIconUrl())
                        .WithTitle("Ignored Channels")
                        .WithDescription(pageDescription[j])
                        .WithFooter($"Page {j + 1}/{pageDescription.Count} ({"entry".ToQuantity(count)})");

                    pages.Add(new Page().WithEmbeds(embed));
                }

                return Paginate(pages, startIndex: page - 1);
            }
        }

#if DEBUG
        [Command("xphack")]
        [Description("Gives a member a certain amount of XP in debug mode.")]
        public async Task<DiscordCommandResult> XpHackAsync(int amount, IMember? member = null)
        {
            member ??= Context.Author;

            var rank = await _db.GetRankAsync(member);

            if (rank is null)
            {
                if (member.Id == Context.Author.Id)
                    return Reply("You don't have a rank on this server yet.");
                else
                    return Reply("This member has no rank on this server.");
            }

            rank.ExperiencePoints += amount;

            return Reply($"You got **{amount}** XP.").RunWith(_db.SaveChangesAsync());
        }
#endif
    }
}
