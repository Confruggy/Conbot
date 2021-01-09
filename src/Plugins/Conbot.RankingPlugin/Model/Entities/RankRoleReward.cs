using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using SQLite;

namespace Conbot.RankingPlugin
{
    public class RankRoleReward
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public ulong GuildId { get; set; }
        public virtual RankGuildConfiguration GuildConfiguration { get; set; } = null!;

        public int Level { get; set; }

        [Unique]
        public ulong RoleId { get; set; }

        public RankRoleReward(ulong guildId, int level, ulong roleId)
        {
            GuildId = guildId;
            Level = level;
            RoleId = roleId;
        }

        public RankRoleReward(RankGuildConfiguration guildConfiguration, int level, ulong roleId)
            : this(guildConfiguration.GuildId, level, roleId) { }
    }
}