using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TRKart.Entities.Models;

namespace TRKart.Entities.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string? FullName { get; set; } = null!;
    }
}