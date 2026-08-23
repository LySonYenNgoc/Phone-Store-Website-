using PhoneStore.Customer.Models;

namespace PhoneStore.Customer.ViewModels
{
    public class ProductListViewModel
    {
        public List<ProductCardViewModel> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Color> Colors { get; set; } = new();
        public List<string> Brands { get; set; } = new();

        // Filters
        public int? CategoryId { get; set; }
        public int? ColorId { get; set; }
        public string Brand { get; set; } = string.Empty;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "name";
        public string Search { get; set; } = string.Empty;

        // Current filter states for view binding
        public string CurrentSearch => Search;
        public int? CurrentCategoryId => CategoryId;
        public int? CurrentColorId => ColorId;
        public string CurrentPriceRange { get; set; } = string.Empty;
        public string CurrentSortBy => SortBy;

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        public int PageSize { get; set; } = 12;
    }
}
