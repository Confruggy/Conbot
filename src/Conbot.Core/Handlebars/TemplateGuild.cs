using System;

using Disqord;
using Disqord.Gateway;

namespace Conbot;

public class TemplateGuild
{
    private readonly IGuild _guild;
    private readonly Lazy<TemplateUser?> _owner;
    private readonly Lazy<TemplateChannel?> _publicUpdatesChannelId;
    private readonly Lazy<TemplateChannel?> _rulesChannelId;
    private readonly Lazy<TemplateChannel?> _systemChannelId;
    private readonly Lazy<TemplateChannel?> _widgetChannelId;

    public ulong Id => _guild.Id;
    public bool IsBoostProgressBarEnabled => _guild.IsBoostProgressBarEnabled;
    public bool IsWidgetEnabled => _guild.IsWidgetEnabled;
    public string Name => _guild.Name;
    public ulong OwnerId => _guild.OwnerId;
    public TemplateUser? Owner => _owner.Value;
    public ulong? PublicUpdatesChannelId => _guild.PublicUpdatesChannelId;
    public TemplateChannel? PublicUpdatesChannel => _publicUpdatesChannelId.Value;
    public ulong? RulesChannelId => _guild.RulesChannelId;
    public TemplateChannel? RulesChannel => _rulesChannelId.Value;
    public ulong? SystemChannelId => _guild.SystemChannelId;
    public TemplateChannel? SystemChannel => _systemChannelId.Value;
    public string VanityurlCode => _guild.VanityUrlCode;
    public ulong? WidgetChannelId => _guild.WidgetChannelId;
    public TemplateChannel? WidgetChannel => _widgetChannelId.Value;

    public TemplateGuild(IGuild guild)
    {
        _guild = guild;

        _owner = new Lazy<TemplateUser?>(() =>
        {
            var member = (_guild as CachedGuild)?.GetMember(OwnerId);
            return member is not null ? new TemplateUser(member) : null;
        });

        _publicUpdatesChannelId = new Lazy<TemplateChannel?>(() =>
        {
            if (PublicUpdatesChannelId is null)
                return null;

            var channel = (_guild as CachedGuild)?.GetChannel(PublicUpdatesChannelId.Value);
            return channel is not null ? new TemplateChannel(channel) : null;
        });

        _rulesChannelId = new Lazy<TemplateChannel?>(() =>
        {
            if (RulesChannelId is null)
                return null;

            var channel = (_guild as CachedGuild)?.GetChannel(RulesChannelId.Value);
            return channel is not null ? new TemplateChannel(channel) : null;
        });

        _systemChannelId = new Lazy<TemplateChannel?>(() =>
        {
            if (SystemChannelId is null)
                return null;

            var channel = (_guild as CachedGuild)?.GetChannel(SystemChannelId.Value);
            return channel is not null ? new TemplateChannel(channel) : null;
        });

        _widgetChannelId = new Lazy<TemplateChannel?>(() =>
        {
            if (WidgetChannelId is null)
                return null;

            var channel = (_guild as CachedGuild)?.GetChannel(WidgetChannelId.Value);
            return channel is not null ? new TemplateChannel(channel) : null;
        });
    }

    public override string ToString() => _guild.ToString()!;
}