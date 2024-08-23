using System.ComponentModel.DataAnnotations;

namespace expenses.Models
{
    public class ExpenseModel
    {
        public int Id { get; set; }
        [Required]
        public string Expense { get; set; }
        [Required]
        public int Price { get; set; }
        public DateTime ExpenseDate { get; set; }
        public int UserId { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public string CategoryName { get; set; }
        public List<CategoryModel> Categories { get; set; }
    }
}
