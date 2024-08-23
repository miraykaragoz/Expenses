using System.ComponentModel.DataAnnotations;

namespace expenses.Models
{
    public class IncomeModel
    {
        public int Id { get; set; }
        [Required]
        public string Income { get; set; }
        [Required]
        public int Price { get; set; }
        public string IncomeDate { get; set; }
        public int UserId { get; set; }
    }
}
