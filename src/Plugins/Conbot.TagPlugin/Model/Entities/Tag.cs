using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.TagPlugin
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
        [Required]
        public ulong CreatorId { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [Required]
        public ulong CreationGuildId { get; set; }
        [Required]
        public ulong CreationChannelId { get; set; }
        public ulong? CreationMessageId { get; set; }
        public ulong? CreationInteractionId { get; set; }

        public virtual List<TagAlias> Aliases { get; set; }
        public virtual List<TagModification> Modifications { get; set; }
        public virtual List<TagUse> Uses { get; set; }
        public virtual List<TagOwnerChange> OwnerChanges { get; set; }

        [NotMapped]
        public string CreationUrl => $"https://discordapp.com/channels/{CreationGuildId}/{CreationChannelId}/{CreationMessageId ?? CreationInteractionId}";
    }
}