namespace PhoneStore.Customer.ViewModels
{    public class ProductCardViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string PrimaryImageUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
        public decimal DiscountPercentage => HasDiscount ? (int)Math.Round((1 - (DiscountPrice!.Value / Price)) * 100) : 0;
    }
}
