using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Discord;

namespace Conbot.ModerationPlugin
{
    public class ModerationGuildConfiguration
    {
        [Key]
        public ulong GuildId { get; set; }

        public ulong? RoleId { get; set; }

        public virtual List<TemporaryMutedUser> TemporaryMutedUsers { get; set; } = null!;

        public ModerationGuildConfiguration(ulong guildId) => GuildId = guildId;

        public ModerationGuildConfiguration(IGuild guild)
            : this(guild.Id) { }
    }
}
