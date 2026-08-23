using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("Categories")]
    public class Category
    {        [Key]
        public int CategoryId { get; set; }

        [StringLength(100)]
        public string? CategoryName { get; set; }        // Alias for easier access in views
        public string? Name => CategoryName;

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
