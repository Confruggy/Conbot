using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Interactive;

using Discord;
using Discord.WebSocket;

using Humanizer;

using Qmmands;

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
    public class RankingModule : DiscordModuleBase
    {
        private readonly RankingContext _db;
        private readonly RankingService _rankingService;
        private readonly InteractiveService _interactiveService;
        private readonly IConfiguration _config;

        public RankingModule(RankingContext context, RankingService service, InteractiveService interactiveService,
            IConfiguration config)
        {
            _db = context;
            _rankingService = service;
            _interactiveService = interactiveService;
            _config = config;
        }

        [Command("show", "")]
        [Description("Shows your or someone else's rank in the server.")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task RankAsync([Description("The member to show the rank of.")] IGuildUser? member = null)
        {
            member ??= (SocketGuildUser)Context.User;

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

            if (foundRank == null)
            {
                if (member.Id == Context.User.Id)
                    await ReplyAsync("You don't have a rank on this server yet.");
                else
                    await ReplyAsync("This member has no rank on this server.");

                return;
            }

            var embed = CreateRankEmbed(member, foundRank.ExperiencePoints, foundRank.RankedMessages, index + 1, total);
            await ReplyAsync(embed: embed);
        }

        public Embed CreateRankEmbed(IGuildUser user, int experiencePoints, int messages, int rank, int total)
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
                .Append(Format.Bold(level.ToString()))
                .Append(" (")
                .Append(percentage.ToString("P2", CultureInfo.InvariantCulture))
                .Append(')');

            var embed = new EmbedBuilder()
                .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
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

            return embed.Build();
        }

        [Command("leaderboard", "list", "all")]
        [Description("Shows the leaderboard of the server.")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(
            ChannelPermission.AddReactions |
            ChannelPermission.EmbedLinks |
            ChannelPermission.UseExternalEmojis)]
        public async Task ListAsync([Description("The page to start with.")] int page = 1)
        {
            var ranks = await _db.GetRanksAsync(Context.Guild!).ToArrayAsync();

            var text = new StringBuilder();
            var pages = new List<string>();

            int padding = ranks.Length.ToString().Length;

            for (int i = 0; i < ranks.Length; i++)
            {
                var rank = ranks[i];

                var user = Context.Guild.GetUser(rank.UserId);
                if (user == null)
                    return;

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
                    pages.Add(text.ToString());
                    text.Clear();
                }
            }

            var paginator = new Paginator();

            for (int i = 0; i < pages.Count; i++)
            {
                var embed = new EmbedBuilder()
                    .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                    .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                    .WithTitle("Leaderboard")
                    .WithDescription(pages[i])
                    .WithFooter($"Page {i + 1}/{pages.Count} ({"entry".ToQuantity(ranks.Length)})");

                paginator.AddPage(embed.Build());
            }

            await paginator.RunAsync(_interactiveService, Context, page - 1);
        }

        [Group("announcements")]
        [Description("Configures level up announcements.")]
        public class Announcements : DiscordModuleBase
        {
            private readonly RankingContext _db;
            private readonly IConfiguration _config;

            public Announcements(RankingContext context, IConfiguration config)
            {
                _db = context;
                _config = config;
            }

            [Command("toggle", "")]
            [Description("Toggles level up announcements.")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ToggleAsync(
                [Description("Wether to enable or disable level up announcements.")]
                [Choices("enable", "disable")]
                string toggle)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);

                string? text = null;

                switch (toggle)
                {
                    case "enable":
                        if (config.ShowLevelUpAnnouncements == true)
                        {
                            await ReplyAsync("Level up announcements are already enabled.");
                            return;
                        }

                        config.ShowLevelUpAnnouncements = true;
                        text = "Level up announcements have been enabled.";

                        break;
                    case "disable":
                        if (config.ShowLevelUpAnnouncements == false)
                        {
                            await ReplyAsync("Level up announcements are already disabled.");
                            return;
                        }

                        config.ShowLevelUpAnnouncements = false;
                        text = "Level up announcements have been disabled.";

                        break;
                }

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(text)
                );
            }

            [Command("channel")]
            [Description("Sets a channel for level up announcements.")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task ChannelAsync(
                [Description("The text channel for level up announcements.")]
                [Remarks(
                    "If you leave this blank, then the channel will be set to **Current Channel** which will send " +
                    "level up announcements to the channel where the member achieved the level up.")]
                ITextChannel? channel = null)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);
                config.LevelUpAnnouncementsChannelId = channel?.Id;

                string text;

                if (channel != null)
                    text = $"Channel for level up announcements has been set to {channel.Mention}.";
                else
                    text = "Channel for level up announcements has been set to **Current Channel**.";

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(text)
                );
            }

            [Command("minimumlevel", "minlevel")]
            [Description("Sets a minimum level for level up announcements.")]
            [Remarks("This is useful to avoid spamming with lower levels where it's faster to achieve a level up.")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task MinimumLevelAsync(
                [Description("The minimum level.")]
                [Remarks(
                    "If you leave this blank, then the minimum level will set to **None** which will send level up " +
                    "announcements for all levels.")]
                [MinValue(1), MaxValue(1000)]
                int? level = null)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);
                config.LevelUpAnnouncementsMinimumLevel = level;

                string text = level != null
                    ? $"Minimum level for level up announcements has been set to **{level}**."
                    : "Minimum level for level up announcements has been set to **None**.";

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(text)
                );
            }

            [Command("settings")]
            [Description("Shows the current settings for level up announcements.")]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            public async Task SettingsAsync(
                [Description("Wether you want to show the server's or your personal settings.")]
                [Remarks("You can only view the server settings if you have the **Manage Server** permission.")]
                [Choices("server", "user")]
                string type = "server")
            {
                Embed embed;

                if (type == "server")
                {
                    if (Context.User is not SocketGuildUser user)
                    {
                        await ReplyAsync(
                            "You can only view the server's settings for level up announcements on a server.");
                        return;
                    }

                    if (!user.GuildPermissions.Has(GuildPermission.ManageGuild))
                    {
                        await ReplyAsync(
                            "You require the **Manage Server** permission to view the server's settings for level up " +
                            "announcements.");
                        return;
                    }

                    var config = await _db.GetGuildConfigurationAsync(Context.Guild!);

                    string enabled = config?.ShowLevelUpAnnouncements == true
                        ? "Enabled"
                        : "Disabled";
                    string channel = config?.LevelUpAnnouncementsChannelId != null
                        ? MentionUtils.MentionChannel(config.LevelUpAnnouncementsChannelId.Value)
                        : "Current Channel";
                    string minimumlevel = config?.LevelUpAnnouncementsMinimumLevel != null
                        ? config.LevelUpAnnouncementsMinimumLevel.ToString()!
                        : "None";

                    embed = new EmbedBuilder()
                        .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                        .WithTitle("Level Up Announcements Server Settings")
                        .AddField("Level Up Announcements", enabled)
                        .AddField("Announcements Channel", channel)
                        .AddField("Minimum Level", minimumlevel)
                        .WithFooter(Context.Guild.ToString(), Context.Guild.IconUrl)
                        .Build();
                }
                else
                {
                    var config = await _db.GetUserConfigurationAsync(Context.User);

                    string allowMentions = config?.AnnouncementsAllowMentions == true
                        ? "Enabled"
                        : "Disabled";
                    string directMessages = config?.AnnouncementsSendDirectMessages != null
                        ? "Enabled"
                        : "Disabled";

                    embed = new EmbedBuilder()
                        .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                        .WithTitle("Level Up Announcements User Settings")
                        .AddField("Mention Notifications", allowMentions)
                        .AddField("Direct Messages", directMessages)
                        .WithFooter(Context.User.ToString(), Context.User.GetAvatarUrl())
                        .Build();
                }

                await ReplyAsync(embed: embed);
            }

            [Command("mentions", "notifications")]
            [Description("Toggles notifications for mentions in level up announcements.")]
            [Remarks(
                "This setting is per user and global. If you enable this setting, you'll receive a notification when " +
                "you get mentioned in level up announcements. Disabling this setting will still send level up " +
                "announcements if the server has them enabled; however, you won't get a notification that you got " +
                "mentioned.")]
            public async Task MentionAsync(
                [Description("Wether to enable or disable notifications for mentions in level up announcements.")]
                [Choices("enable", "disable")]
                string toggle)
            {
                var config = await _db.GetOrCreateUserConfigurationAsync(Context.User);

                string? text = null;

                switch (toggle)
                {
                    case "enable":
                        if (config.AnnouncementsAllowMentions ?? false)
                        {
                            await ReplyAsync(
                                "You already receive notifications when you get mentioned in level up announcements.");
                            return;
                        }

                        config.AnnouncementsAllowMentions = true;
                        text =
                            "You'll now receive notifications when you get mentioned in level up announcements.";

                        break;
                    case "disable":
                        if (!(config.AnnouncementsAllowMentions ?? false))
                        {
                            await ReplyAsync(
                                "You already don't receive notifications when you get mentioned in level up announcements.");
                            return;
                        }

                        config.AnnouncementsAllowMentions = false;
                        text =
                            "You'll no longer receive notifications when you get mentioned in level up announcements.";

                        break;
                }

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(text)
                );
            }

            [Command("directmessages")]
            [Description("Toggles direct messages for level up announcements.")]
            [Remarks(
                "This setting is per user and global. If you enable this setting, you'll receive a direct message " +
                "whenever you achieved a level up on any server. However, it will only send a direct message if " +
                "level up announcements are *not* enabled on the server or you don't meet the minimum level " +
                "specified by the server. If a server has level up announcements enabled and you meet the minimum " +
                "level, then they will be sent on the server instead.")]
            public async Task DirectMessagesAsync(
                [Description("Wether to enable or disable direct messages for level up announcements.")]
                [Choices("enable", "disable")]
                string toggle)
            {
                var config = await _db.GetOrCreateUserConfigurationAsync(Context.User);

                string? text = null;

                switch (toggle)
                {
                    case "enable":
                        if (config.AnnouncementsSendDirectMessages ?? false)
                        {
                            await ReplyAsync(
                                "You already receive direct messages for level up announcements.");
                            return;
                        }

                        config.AnnouncementsSendDirectMessages = true;
                        text =
                            "You'll now receive direct messages for level up announcements if a server has them disabled.";

                        break;
                    case "disable":
                        if (!(config.AnnouncementsSendDirectMessages ?? false))
                        {
                            await ReplyAsync(
                                "You already don't receive direct messages for level up announcements.");
                            return;
                        }

                        config.AnnouncementsSendDirectMessages = false;
                        text =
                            "You'll no longer receive direct messages for level up announcements.";

                        break;
                }

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(text)
                );
            }
        }

        [Group("rolerewards", "role", "rewards")]
        [Description("Configures automated role rewards.")]
        [Remarks(
            "You can set roles which will be automatically given to a member when they reach a specific " +
            "level. Roles of a member will only update after they gained XP.")]
        [RequireContext(ContextType.Guild)]
        public class RoleRewardsCommands : DiscordModuleBase
        {
            private readonly RankingContext _db;
            private readonly RankingService _rankingService;
            private readonly InteractiveService _interactiveService;
            private readonly IConfiguration _config;

            public RoleRewardsCommands(RankingContext context, RankingService rankingService,
                InteractiveService interactiveService, IConfiguration config)
            {
                _db = context;
                _rankingService = rankingService;
                _interactiveService = interactiveService;
                _config = config;
            }

            [Command("list", "all", "")]
            [Description("Lists all available role rewards.")]
            [RequireBotPermission(
                ChannelPermission.AddReactions |
                ChannelPermission.EmbedLinks |
                ChannelPermission.UseExternalEmojis)]
            public async Task ListAsync([Description("The page to start with.")] int page = 1)
            {
                var roleRewards = await _db.GetRoleRewardsAsync(Context.Guild!).ToListAsync();
                int count = roleRewards.Count;

                if (count == 0)
                {
                    await ReplyAsync("There are no rewards available for this server.");
                    return;
                }

                int currentPage = 1;
                int totalPages = (count / 10) + (count % 10 != 0 ? 1 : 0);

                var paginator = new Paginator();
                var embed = new EmbedBuilder();

                for (int i = 0; i < count; i++)
                {
                    var roleReward = roleRewards[i];
                    int level = roleReward.Level;
                    int experiencePoints = _rankingService.GetTotalExperiencePoints(level);

                    embed.AddField(
                        $"Level {level} ({experiencePoints.ToString("n0", CultureInfo.CurrentCulture)} XP)",
                        MentionUtils.MentionRole(roleReward.RoleId));

                    if ((i + 1) % 10 == 0 || i == count - 1)
                    {
                        embed
                            .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                            .WithTitle("Role Rewards")
                            .WithFooter($"Page {currentPage}/{totalPages} ({"entry".ToQuantity(count)})")
                            .Build();

                        currentPage++;
                        paginator.AddPage(embed.Build());
                        embed = new EmbedBuilder();
                    }
                }

                await paginator.RunAsync(_interactiveService, Context, page - 1);
            }

            [Command("add")]
            [Description("Adds a role reward for a specific level.")]
            [Remarks(
                "Only one role can be rewarded for a specific level and only one level can be set for a specific role.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task AddAsync(
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
                if (roleReward != null)
                {
                    await ReplyAsync("There already exists a reward for this level.");
                    return;
                }

                roleReward = await _db.GetRoleRewardAsync(role);
                if (roleReward != null)
                {
                    await ReplyAsync("There already exists a reward for this role.");
                    return;
                }

                await _db.AddRoleRewardAsync(config, level, role);

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(
                        $"Role {role.Mention} has been added as a reward for level **{level}**.",
                        allowedMentions: AllowedMentions.None)
                );
            }

            [Command("remove")]
            [Description("Removes a role reward.")]
            [Remarks("This will automatically remove the role from everyone.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RemoveAsync([Description("The role of the reward to remove."), Remainder] IRole role)
            {
                var roleReward = await _db.GetRoleRewardAsync(role);

                if (roleReward == null)
                {
                    await ReplyAsync("There exists no reward for this role.");
                    return;
                }

                _db.RemoveRoleReward(roleReward);

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(
                        $"Role {role.Mention} has been removed as a reward for level **{roleReward.Level}**.",
                        allowedMentions: AllowedMentions.None)
                );
            }

            [Command("type")]
            [Description("Sets the type for role rewards.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task StackAsync(
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
                        {
                            await ReplyAsync("Type for role rewards is already set to **stack**.");
                            return;
                        }

                        config.RoleRewardsType = RoleRewardsType.Stack;
                        text = "Type for role rewards has been set to **stack**.";
                        break;
                    case "remove":
                        if (config.RoleRewardsType == RoleRewardsType.Remove)
                        {
                            await ReplyAsync("Type for role rewards is already set to **remove**.");
                            return;
                        }

                        config.RoleRewardsType = RoleRewardsType.Remove;
                        text = "Type for role rewards has been set to **remove**.";
                        break;
                }

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync(text)
                );
            }

            [Command("settings")]
            [Description("Shows the current settings for role rewards.")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.EmbedLinks)]
            public async Task SettingsAsync()
            {
                var config = await _db.GetGuildConfigurationAsync(Context.Guild!);

                var type = config?.RoleRewardsType ?? RoleRewardsType.Stack;

                string typeText = type == RoleRewardsType.Stack
                    ? "Stack (members will keep all previous rewards)"
                    : "Remove (members will only keep the highest reward)";

                var embed = new EmbedBuilder()
                    .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                    .WithTitle("Role Rewards Settings")
                    .AddField("Type", typeText)
                    .Build();

                await ReplyAsync(embed: embed);
            }
        }

        [Group("ignore")]
        [Description("Configures channels to ignore from gaining experience points.")]
        [Remarks(
            "Ignored channels are useful to avoid gaining experience points from channels where usually no " +
            "actual discussion is held, e.g., bot channels.")]
        public class IgnoredChannelsCommands : DiscordModuleBase
        {
            private readonly RankingContext _db;
            private readonly InteractiveService _interactiveService;
            private readonly IConfiguration _config;

            public IgnoredChannelsCommands(RankingContext context, InteractiveService interactiveService,
                IConfiguration config)
            {
                _db = context;
                _interactiveService = interactiveService;
                _config = config;
            }

            [Command("add", "")]
            [Description("Adds a channel to ignore.")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
            public async Task AddAsync([Description("The text channel to ignore.")] ITextChannel channel)
            {
                var config = await _db.GetOrCreateGuildConfigurationAsync(Context.Guild!);

                var ignoredChannel = await _db.GetIgnoredChannelAsync(channel);
                if (ignoredChannel != null)
                {
                    await ReplyAsync("This channel is already ignored.");
                    return;
                }

                _db.AddIgnoredChannel(channel, config);

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync($"Channel {channel.Mention} is now ignored from gaining experience points.")
                );
            }

            [Command("remove")]
            [Description("Removes a channel from being ignored.")]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
            public async Task RemoveAsync(
                [Description("The text channel to remove from being ignored.")] ITextChannel channel)
            {
                var ignoredChannel = await _db.GetIgnoredChannelAsync(channel);

                if (ignoredChannel == null)
                {
                    await ReplyAsync("This channel isn't ignored.");
                    return;
                }

                _db.RemoveIgnoredChannel(ignoredChannel);

                await Task.WhenAll(
                    _db.SaveChangesAsync(),
                    ReplyAsync($"Channel {channel.Mention} is now no longer ignored from gaining experience points.")
                );
            }

            [Command("list", "all")]
            [Description("Lists all ignored channels.")]
            [RequireBotPermission(
                ChannelPermission.AddReactions |
                ChannelPermission.EmbedLinks |
                ChannelPermission.UseExternalEmojis)]
            public async Task ListAsync([Description("The page to start with.")] int page = 1)
            {
                var ignoredChannels = await _db.GetIgnoredChannelsAsync(Context.Guild!).ToArrayAsync();
                int count = ignoredChannels.Length;

                if (count == 0)
                {
                    await ReplyAsync("There are no channels ignored from gaining experience points.");
                    return;
                }

                List<string> pages = new();

                int i = 1;
                var pageText = new StringBuilder();

                foreach (var ignoredChannel in ignoredChannels)
                {
                    pageText.AppendLine(MentionUtils.MentionChannel(ignoredChannel.ChannelId));

                    if (i % 15 == 0 || i == count)
                    {
                        pages.Add(pageText.ToString());
                        pageText.Clear();
                    }

                    i++;
                }

                if (page > pages.Count || page < 1)
                {
                    await ReplyAsync("This page doesn't exist.");
                    return;
                }

                var paginator = new Paginator();

                for (int j = 0; j < pages.Count; j++)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(_config.GetValue<uint>("DefaultEmbedColor"))
                        .WithAuthor(Context.Guild!.Name, Context.Guild!.IconUrl)
                        .WithTitle("Ignored Channels")
                        .WithDescription(pages[j])
                        .WithFooter($"Page {j + 1}/{pages.Count} ({"entry".ToQuantity(count)})")
                        .Build();
                    paginator.AddPage(embed);
                }

                await paginator.RunAsync(_interactiveService, Context, page - 1);
            }
        }

#if DEBUG
        [Command("xphack")]
        [Description("Gives a member a certain amount of XP in debug mode.")]
        public async Task XpHackAsync(int amount, IGuildUser? member = null)
        {
            member ??= (SocketGuildUser)Context.User;

            var rank = await _db.GetRankAsync(member);

            if (rank == null)
            {
                if (member.Id == Context.User.Id)
                    await ReplyAsync("You don't have a rank on this server yet.");
                else
                    await ReplyAsync("This member has no rank on this server.");

                return;
            }

            rank.ExperiencePoints += amount;

            await _db.SaveChangesAsync();
            await ReplyAsync($"You got **{amount}** XP.");
        }
#endif
    }
}
