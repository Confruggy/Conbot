using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.RankingPlugin;

public class Rank
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public bool IsBot { get; set; }
    public int ExperiencePoints { get; set; }
    public int RankedMessages { get; set; }
    public int TotalMessages { get; set; }
    public DateTime? LastMessage { get; set; }

    public Rank(ulong guildId, ulong userId, bool isBot, int experiencePoints, int rankedMessages,
        int totalMessages)
    {
        GuildId = guildId;
        UserId = userId;
        IsBot = isBot;
        ExperiencePoints = experiencePoints;
        RankedMessages = rankedMessages;
        TotalMessages = totalMessages;
    }
}