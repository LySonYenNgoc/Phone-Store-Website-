using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        [Column("CustomerID")]
        public int CustomerId { get; set; }

        public int Id => CustomerId; // Alias for easier access

        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        public int? BirthYear { get; set; }

        public DateTime? CreatedDate { get; set; }

        [Column("MembershipID")]
        public int? MembershipId { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
