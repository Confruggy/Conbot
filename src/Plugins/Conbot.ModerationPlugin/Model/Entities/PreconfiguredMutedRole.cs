using System.ComponentModel.DataAnnotations;

using Discord;

namespace Conbot.ModerationPlugin
{
    public class PreconfiguredMutedRole
    {
        [Key]
        public ulong RoleId { get; set; }

        public ulong GuildId { get; set; }

        public PreconfiguredMutedRole(ulong roleId, ulong guildId)
        {
            RoleId = roleId;
            GuildId = guildId;
        }

        public PreconfiguredMutedRole(IRole role)
            : this(role.Id, role.Guild.Id) { }
    }
}
