namespace PhoneStore.Customer.ViewModels
{
    public class ProductDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Specifications { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public int StockQuantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<ProductImageViewModel> Images { get; set; } = new();
        public List<ColorViewModel> Colors { get; set; } = new();
        public List<ProductCardViewModel> RelatedProducts { get; set; } = new();
    }

    public class ProductImageViewModel
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public int? ColorId { get; set; }
    }

    public class ColorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
    }
}
