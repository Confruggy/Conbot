using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class Tag
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public ulong GuildId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public ulong OwnerId { get; set; }

        public virtual List<TagAlias> Aliases { get; set; }

        public virtual TagCreation Creation { get; set; }

        public virtual List<TagModification> Modifications { get; set; }
        public virtual List<TagUse> Uses { get; set; }
    }
}