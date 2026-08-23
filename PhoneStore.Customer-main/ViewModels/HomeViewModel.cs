namespace PhoneStore.Customer.ViewModels
{
    public class HomeViewModel
    {
        public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
        public List<ProductCardViewModel> LatestProducts { get; set; } = new();
        public List<ProductCardViewModel> BestSellingProducts { get; set; } = new();
        public List<CategoryViewModel> Categories { get; set; } = new();
    }    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
