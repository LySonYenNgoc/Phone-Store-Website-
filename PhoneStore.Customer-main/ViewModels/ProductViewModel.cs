using PhoneStore.Customer.Models;

namespace PhoneStore.Customer.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // Computed properties
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
        public decimal DiscountPercentage => HasDiscount ? Math.Round((1 - (DiscountPrice!.Value / Price)) * 100, 0) : 0;
        public bool InStock => Stock > 0;

        // Navigation properties
        public Category? Category { get; set; }
        public Color? Color { get; set; }
        public List<ProductImageViewModel> Images { get; set; } = new();
    }
}
