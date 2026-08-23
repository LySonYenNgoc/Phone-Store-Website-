using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [Column("OrderID")]
        public int OrderId { get; set; }

        [Column("CustomerID")]
        public int? CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }

        public string? Status { get; set; }

        public string? PaymentMethod { get; set; }

        public string? Notes { get; set; }

        public DateTime? OrderDate { get; set; }

        [Column("CouponID")]
        public int? CouponId { get; set; }

        [Column("ShippingAddressID")]
        public int? ShippingAddressId { get; set; }

        // Navigation properties
        public virtual CustomerEntity? Customer { get; set; }
        public virtual ShippingAddress? ShippingAddress { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
