using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conbot.Data.Entities
{
    public class Modification : Message
    {
        public DateTimeOffset ModifiedAt { get; set; }
    }
}