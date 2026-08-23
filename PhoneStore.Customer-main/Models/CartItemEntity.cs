using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    public class CartItemEntity
    {
        [Key]
        public int CartItemId { get; set; }

        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public int? ColorId { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        [ForeignKey("CustomerId")]
        public virtual CustomerEntity Customer { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("ColorId")]
        public virtual Color? Color { get; set; }
    }
}
