using System.ComponentModel.DataAnnotations;

namespace FinGrid.Models
{
    public class BankTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BankAccountId { get; set; } // Прив'язка до конкретної картки

        [Required]
        public string ExternalId { get; set; } // ID транзакції від банку

        public long Time { get; set; } // Unix-час транзакції
        public string Description { get; set; } // Назва магазину або послуги
        public int Mcc { get; set; } // Код категорії (їжа, авто, тощо)

        [Required]
        public decimal Amount { get; set; } // Сума (вже в гривнях)
        public decimal CommissionRate { get; set; }
        public decimal CashbackAmount { get; set; }
        public decimal BalanceAfter { get; set; } // Залишок після операції

        // Навігаційна властивість
        public BankAccount BankAccount { get; set; }
    }
}