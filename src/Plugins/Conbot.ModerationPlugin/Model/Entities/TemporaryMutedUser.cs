using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.ModerationPlugin;

public class TemporaryMutedUser
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public virtual ModerationGuildConfiguration GuildConfiguration { get; set; } = null!;

    public ulong UserId { get; set; }
    public ulong RoleId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime EndsAt { get; set; }

    public TemporaryMutedUser(ulong guildId, ulong userId, ulong roleId, DateTime startedAt, DateTime endsAt)
    {
        GuildId = guildId;
        UserId = userId;
        RoleId = roleId;
        StartedAt = startedAt;
        EndsAt = endsAt;
    }
}