using System.ComponentModel.DataAnnotations;

namespace Conbot.Data.Entities
{
    public class TagCreation : Creation
    {
        [Key]
        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}