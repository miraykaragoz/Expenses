using System.ComponentModel.DataAnnotations;

namespace expenses.Models
{
    public class CategoryModel
    {
        public int Id { get; set; }
        [Required]
        public string Category { get; set; }
        public int UserId { get; set; }
    }
}
