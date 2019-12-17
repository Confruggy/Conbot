using System.ComponentModel.DataAnnotations;

namespace Conbot.Data.Entities
{
    public class TagAliasCreation : Creation
    {
        [Key]
        public int TagAliasId { get; set; }
        public TagAlias TagAlias { get; set; }

    }
}