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
        public virtual RankGuildConfiguration GuildConfiguration { get; set; }
        public int Level { get; set; }
        [Unique]
        public ulong RoleId { get; set; }
    }
}