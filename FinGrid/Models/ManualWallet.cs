namespace FinGrid.Models
{
    public class ManualWallet
    {
        public int Id { get; set; }
        public string Name { get; set; } // "Готівка", "Сейф"
        public decimal Balance { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
