namespace FinGrid.Models
{
    public class BudgetCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal MonthlyLimit { get; set; }
        public string Icon { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
