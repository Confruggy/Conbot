using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class TagModification : Modification
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public new ulong GuildId { get; set; }

        [Required]
        public int TagId { get; set; }
        public Tag Tag { get; set; }

        [Required]
        public string NewContent { get; set; }
        [Required]
        public string OldContent { get; set; }
    }
}