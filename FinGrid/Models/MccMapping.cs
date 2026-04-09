namespace FinGrid.Models
{
    public class MccMapping
    {
        public int Id { get; set; }
        public int Mcc { get; set; } // Код категорії продавця (напр. 5812)
        public int CategoryId { get; set; } // Твоя категорія (напр. "Ресторани")
        public BudgetCategory Category { get; set; }
        public string UserId { get; set; }
    }
}
