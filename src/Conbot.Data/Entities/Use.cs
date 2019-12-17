using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class Use : Message
    {
        public DateTimeOffset UsedAt { get; set; }
    }
}