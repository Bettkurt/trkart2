using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TRKart.Entities.Models;

namespace TRKart.Entities.DTOs
{
    public class CustomerDataDto
    {
        [Required]
        public int CustomerID { get; set; }
        [Required]
        public string CustomerNumber { get; set; }
        [Required]
        public string Email { get; set; }
        public string? FullName { get; set; }
    }
}