using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("ShippingAddresses")]
    public class ShippingAddress
    {
        [Key]
        [Column("AddressID")]
        public int AddressId { get; set; }

        [Column("CustomerID")]
        public int? CustomerId { get; set; }

        [StringLength(100)]
        public string? RecipientName { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? AddressLine { get; set; }

        [StringLength(100)]
        public string? Ward { get; set; }

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        public bool? IsDefault { get; set; }

        // Navigation properties
        public virtual CustomerEntity? Customer { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
