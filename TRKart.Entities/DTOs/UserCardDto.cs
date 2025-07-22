using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TRKart.Entities.DTOs
{
    public class UserCardDto
    {
        [Required]
        public int CardId { get; set; }
        public int CustomerId { get; set; }
        public decimal Balance { get; set; }

        [JsonIgnore] // Hides CardNumber from API responses
        public string CardNumber { get; set; } = null!;
    }
}