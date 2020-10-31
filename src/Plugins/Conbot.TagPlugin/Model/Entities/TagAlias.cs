using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
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
        public virtual Tag Tag { get; set; }

        public virtual TagAliasCreation Creation { get; set; }

        public virtual List<TagUse> TagUses { get; set; }
    }
}
