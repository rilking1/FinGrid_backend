namespace FinGrid.Models
{
    public class ManualTransactionDto
    {
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; } // true - прибуток, false - витрата
        public int? CategoryId { get; set; } // Опціонально для витрат
        public string? Description { get; set; }
    }
}