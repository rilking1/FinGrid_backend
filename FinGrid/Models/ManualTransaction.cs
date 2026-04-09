namespace FinGrid.Models
{
    public class ManualTransaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }

        public int? CategoryId { get; set; }
        // ДОДАЙ ЦЕЙ РЯДОК:
        public virtual BudgetCategory? Category { get; set; }

        public int WalletId { get; set; }
        public string UserId { get; set; }
    }
}
