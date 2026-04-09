using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinGrid.Models
{
    public class BankAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Прив'язка до твого користувача Identity

        [Required]
        public string ExternalId { get; set; } // ID рахунку, який дає Monobank (для синхронізації)

        public string? Name { get; set; } // Назва (напр. "Чорна картка")

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Balance { get; set; } // Баланс у гривнях/валюті

        public int CurrencyCode { get; set; } // 980 - UAH, 840 - USD тощо

        public string? Type { get; set; } // "black", "white", "fop" тощо

        public string? Iban { get; set; }

        public DateTime LastUpdated { get; set; }

        // Навігаційна властивість
        public User User { get; set; }
        public bool IsIncludedInTotal { get; set; } = true;
    }
}