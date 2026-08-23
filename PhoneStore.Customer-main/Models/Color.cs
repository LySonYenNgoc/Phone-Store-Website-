using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("Colors")]
    public class Color
    {        [Key]
        public int ColorId { get; set; }

        [StringLength(100)]
        public string? ColorName { get; set; }        // Alias for easier access in views
        public string? Name => ColorName;

        // Navigation properties
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
