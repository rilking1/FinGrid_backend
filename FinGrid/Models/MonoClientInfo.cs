namespace FinGrid.Models
{
    public class MonoClientInfo
    {
        public string ClientId { get; set; }
        public string Name { get; set; }
        public List<MonoAccountDto> Accounts { get; set; }
    }

    public class MonoAccountDto
    {
        public string Id { get; set; }
        public long Balance { get; set; } // Баланс у копійках
        public long CreditLimit { get; set; }
        public int CurrencyCode { get; set; }
        public string Type { get; set; }
        public string Iban { get; set; }
    }
}