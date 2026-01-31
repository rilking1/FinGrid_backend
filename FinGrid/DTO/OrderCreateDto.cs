using System.ComponentModel.DataAnnotations;

namespace FinGrid.DTO
{
    public class OrderCreateDto
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsNegotiable { get; set; }
        public string Dorm { get; set; }
        public string Room { get; set; }
    }
}