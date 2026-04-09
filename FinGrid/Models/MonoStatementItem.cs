namespace FinGrid.Models
{
    public class MonoStatementItem
    {
        public string Id { get; set; }
        public long Time { get; set; }
        public string Description { get; set; }
        public int Mcc { get; set; }
        public int OriginalMcc { get; set; }
        public bool Hold { get; set; }
        public long Amount { get; set; }
        public long OperationAmount { get; set; }
        public int CurrencyCode { get; set; }
        public long CommissionRate { get; set; }
        public long CashbackAmount { get; set; }
        public long Balance { get; set; }
        public string Comment { get; set; }
        public string ReceiptId { get; set; }
        public string CounterEdrpou { get; set; }
        public string CounterIban { get; set; }
        public string CounterName { get; set; }
    }
}