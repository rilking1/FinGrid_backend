namespace FinGrid.Models
{
    public class BankConnection
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Прив'язка до IdentityUser
        public string PublicToken { get; set; } // Твій персональний токен Monobank
        public DateTime LastSync { get; set; }

        // Навігаційна властивість
        public User User { get; set; }
    }
}
