namespace TRKart.Entities.DTOs
{
    public class TransactionCreateDto
    {
        public int CardID { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; } = null!;
    }
} 