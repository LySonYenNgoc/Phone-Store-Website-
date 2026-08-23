using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        public int Id => ProductId; // Alias for easier access

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        // Alias for easier access in views
        public string Name => ProductName;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? DetailDescription { get; set; }

        // Alias for easier access
        public string? Description => ShortDescription;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public int StockQuantity => Stock; // Alias for easier access

        public int CategoryId { get; set; }        public int? DiscountId { get; set; }        [Required]
        public bool IsPublished { get; set; } = true;

        // Thông tin bảo hành
        [Range(1, 60, ErrorMessage = "Thời gian bảo hành phải từ 1 đến 60 tháng")]
        public int WarrantyPeriodMonths { get; set; } = 12; // Thời gian bảo hành mặc định 12 tháng

        [StringLength(1000)]
        public string? WarrantyTerms { get; set; } // Điều khoản bảo hành        // Navigation properties
        public virtual Category? Category { get; set; }
        public virtual DiscountProgram? Discount { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // Calculated properties for easier access in views
        public decimal? DiscountPrice => Discount?.DiscountPercent > 0
            ? Price - (Price * Discount.DiscountPercent.Value / 100)
            : null;

        public int DiscountPercentage => Discount?.DiscountPercent ?? 0;

        public string? PrimaryImageUrl => ProductImages?.FirstOrDefault()?.ImageUrl;
    }
}
