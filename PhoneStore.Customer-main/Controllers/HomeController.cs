using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Customer.Models;
using PhoneStore.Customer.ViewModels;

namespace PhoneStore.Customer.Controllers;

public class HomeController : Controller
{
    private readonly PhoneStoreContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(PhoneStoreContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeViewModel();

        // Get featured products (products with discount)
        var featuredProducts = await _context.Products
            .Where(p => p.IsPublished && p.Discount != null)
            .Include(p => p.Category)
            .Include(p => p.Discount)
            .Include(p => p.ProductImages)
            .ThenInclude(pi => pi.Color)
            .Take(8)
            .ToListAsync();        viewModel.FeaturedProducts = featuredProducts.Select(p => new ProductCardViewModel
        {
            ProductId = p.ProductId,
            Name = p.ProductName ?? "",
            Brand = "", // No brand field in simplified model
            Price = p.Price,
            DiscountPrice = p.Discount?.DiscountPercent != null ?
                p.Price * (1 - (decimal)p.Discount.DiscountPercent.Value / 100) : null,
            PrimaryImageUrl = p.ProductImages.Any() ?
                p.ProductImages.First().ImageUrl : "/images/no-image.png",
            CategoryName = p.Category?.CategoryName ?? "",
            ColorName = "" // Simplified color handling
        }).ToList();

        // Get latest products
        var latestProducts = await _context.Products
            .Where(p => p.IsPublished)
            .Include(p => p.Category)
            .Include(p => p.Discount)
            .Include(p => p.ProductImages)
            .ThenInclude(pi => pi.Color)
            .OrderByDescending(p => p.ProductId)
            .Take(8)
            .ToListAsync();        viewModel.LatestProducts = latestProducts.Select(p => new ProductCardViewModel
        {
            ProductId = p.ProductId,
            Name = p.ProductName ?? "",
            Brand = "", // No brand field in simplified model
            Price = p.Price,
            DiscountPrice = p.Discount?.DiscountPercent != null ?
                p.Price * (1 - (decimal)p.Discount.DiscountPercent.Value / 100) : null,
            PrimaryImageUrl = p.ProductImages.Any() ?
                p.ProductImages.First().ImageUrl : "/images/no-image.png",
            CategoryName = p.Category?.CategoryName ?? "",
            ColorName = "" // Simplified color handling
        }).ToList();

        // Get categories
        var categories = await _context.Categories
            .ToListAsync();

        viewModel.Categories = categories
            .Where(c => !string.IsNullOrEmpty(c.CategoryName))
            .Select(c => new CategoryViewModel            {
                CategoryId = c.CategoryId,
                Name = c.CategoryName!,
                ImageUrl = "/images/category-default.png", // No ImageUrl field in simplified model
                ProductCount = _context.Products.Count(p => p.IsPublished && p.CategoryId == c.CategoryId)
            }).ToList();

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string phone, string message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin.";
            return View();
        }

        // Here you would typically save to database or send email
        // For now, we'll just show a success message
        TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
        return RedirectToAction("Contact");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
