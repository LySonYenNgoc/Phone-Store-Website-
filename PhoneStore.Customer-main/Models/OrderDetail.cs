using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        [Column("OrderDetailId")]
        public int OrderDetailId { get; set; }

        [Column("OrderId")]
        public int? OrderId { get; set; }

        [Column("ProductId")]
        public int? ProductId { get; set; }

        [Column("ColorId")]
        public int? ColorId { get; set; }

        [Column("Quantity")]
        public int? Quantity { get; set; }

        [Column("UnitPrice", TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column("TotalPrice", TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
        public virtual Color? Color { get; set; }
        public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
    }
}
