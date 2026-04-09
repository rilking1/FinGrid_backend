namespace FinGrid.DTO
{
    public class TransactionHistoryDto
    {
        public string Id { get; set; } // Текст, бо у банку та ручних можуть бути різні ID
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public long Time { get; set; } // Unix Timestamp для зручного сортування
        public string Source { get; set; } // "Bank" або "Manual"
        public string CategoryName { get; set; }
        public string CategoryIcon { get; set; }
    }
}
