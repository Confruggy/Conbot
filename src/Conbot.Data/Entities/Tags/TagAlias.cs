using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class TagAlias
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public ulong GuildId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public ulong OwnerId { get; set; }

        [Required]
        public int TagId { get; set; }
        public Tag Tag { get; set; }

        public TagAliasCreation Creation { get; set; }

        public List<TagUse> TagUses { get; set; }
    }
}
