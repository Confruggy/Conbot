using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class TagUse : Use
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TagId { get; set; }
        public Tag Tag { get; set; }

        public int? UsedAliasId { get; set; }
        public TagAlias UsedAlias { get; set; }
    }
}