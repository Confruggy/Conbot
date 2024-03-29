using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Extensions;

using Disqord;
using Disqord.Bot;

using Qmmands;

namespace Conbot.InfoPlugin;

[Name("Info")]
public class InfoModule : ConbotModuleBase
{
    [Command("ping")]
    [Description(
        "Sends a message and shows the time difference between the command message and the bots response.")]
    [Remarks("This has nothing to do with your own ping.")]
    public async Task<DiscordCommandResult> PingAsync()
    {
        var message = await Reply("**Pong:** ...");

        long difference = (message.CreatedAt() - Context.Message.CreatedAt()).Milliseconds;

        return Modify(message, $"**Pong:** {difference} ms");
    }

    [Command("uptime")]
    [Description("Shows the uptime of the bot.")]
    public DiscordCommandResult Uptime() => Reply($"The bot is running for {GetUptime().ToLongFormattedString()}.");

    private static TimeSpan GetUptime()
        => DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime);

    [Group("roleinfo")]
    [Description("Gives information about roles.")]
    public class RoleInfoCommands : ConbotGuildModuleBase
    {
        private readonly IConfiguration _config;

        public RoleInfoCommands(IConfiguration configuration) => _config = configuration;

        [Command("summary", "")]
        [Description("Shows a short summary about a specific role.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public DiscordCommandResult Summary([Description("The role to show a summary of.")] IRole role)
        {
            string checkMarkEmote = $"<:{_config.GetValue<string>("Emotes:SettingsCheckMark")}>";
            string crossMarkEmote = $"<:{_config.GetValue<string>("Emotes:SettingsCrossMark")}>";

            var roleSettingsText = new StringBuilder()
                .Append(role.IsHoisted ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Display role members seperately from online members")
                .Append(role.IsMentionable ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Allow anyone to @mention this role");

            var embed = new LocalEmbed()
                .WithColor(role.Color)
                .WithAuthor(role.Name)
                .AddField("Mention", role.Mention, true)
                .AddField("Position", role.Position, true)
                .AddField("Color", role.Color, true)
                .AddField("Role Settings", roleSettingsText.ToString())
                .WithTimestamp(role.CreatedAt())
                .WithFooter($"ID: {role.Id}");

            return Reply(embed);
        }

        [Command("permissions")]
        [Description("Shows the permissions of a specific role.")]
        [OverrideArgumentParser(typeof(InteractiveArgumentParser))]
        public DiscordCommandResult Role([Description("The role to show the permissions of.")] IRole role)
        {
            string checkMarkEmote = $"<:{_config.GetValue<string>("Emotes:SettingsCheckMark")}>";
            string crossMarkEmote = $"<:{_config.GetValue<string>("Emotes:SettingsCrossMark")}>";
            var permission = role.Permissions;

            var generalServerPermissionsText = new StringBuilder()
                .Append(permission.ViewChannels ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" View Channels")
                .Append(permission.ManageChannels ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Channels")
                .Append(permission.ManageRoles ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Roles")
                .Append(permission.ManageEmojisAndStickers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Emojis")
                .Append(permission.ManageEmojisAndStickers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" View Audit Log")
                .Append(permission.ViewAuditLog ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Webhooks")
                .Append(permission.ManageGuild ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Server");

            var membershipPermissionsText = new StringBuilder()
                .Append(permission.CreateInvites ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Create Invite")
                .Append(permission.SetNick ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Change Nickname")
                .Append(permission.ManageNicks ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Nicknames")
                .Append(permission.KickMembers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Kick Members")
                .Append(permission.BanMembers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Ban Members");

            var textChannelPermissionsText = new StringBuilder()
                .Append(permission.SendMessages ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Send Messages")
                .Append(permission.SendEmbeds ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Embed Links")
                .Append(permission.SendAttachments ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Attach Files")
                .Append(permission.AddReactions ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Add Reactions")
                .Append(permission.UseExternalEmojis ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Use External Emojis")
                .Append(permission.MentionEveryone ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Mention @everyone, @here and All Roles")
                .Append(permission.ManageMessages ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Manage Messages")
                .Append(permission.ReadMessageHistory ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Read Message History")
                .Append(permission.UseTextToSpeech ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Send Text-to-speech Messages");

            var voiceChannelPermissionsText = new StringBuilder()
                .Append(permission.Connect ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Connect")
                .Append(permission.Speak ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Speak")
                .Append(permission.UseVoiceActivity ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Use Voice Activity")
                .Append(permission.PrioritySpeaker ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Priority Speaker")
                .Append(permission.MuteMembers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Mute Members")
                .Append(permission.DeafenMembers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Deafen Members")
                .Append(permission.MoveMembers ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Move Members");

            var advancedPermissionsText = new StringBuilder()
                .Append(permission.Administrator ? checkMarkEmote : crossMarkEmote)
                .AppendLine(" Administrator");

            var embed = new LocalEmbed()
                .WithColor(role.Color)
                .WithAuthor(role.Name)
                .AddField("General Server Permissions", generalServerPermissionsText.ToString())
                .AddField("Membership Permissions", membershipPermissionsText.ToString())
                .AddField("Text Channel Permissions", textChannelPermissionsText.ToString())
                .AddField("Voice Channel Permissions", voiceChannelPermissionsText.ToString())
                .AddField("Advanced Permissions", advancedPermissionsText.ToString())
                .WithFooter($"ID: {role.Id}");

            return Reply(embed);
        }
    }
}