using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class Creation : Message
    {
        public DateTimeOffset CreatedAt { get; set; }
    }
}