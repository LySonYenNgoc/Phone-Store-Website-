using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("DiscountPrograms")]
    public class DiscountProgram
    {
        [Key]
        public int DiscountId { get; set; }

        [StringLength(100)]
        public string? DiscountName { get; set; }

        public int? DiscountPercent { get; set; }

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
