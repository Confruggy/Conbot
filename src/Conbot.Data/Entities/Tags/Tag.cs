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

        public List<TagAlias> Aliases { get; set; }

        public TagCreation Creation { get; set; }

        public List<TagModification> Modifications { get; set; }
        public List<TagUse> Uses { get; set; }
    }
}