using Microsoft.EntityFrameworkCore;
using PhoneStore.Customer.Models;

namespace PhoneStore.Customer.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDatabase(PhoneStoreContext context)
        {
            // Check if products already exist
            if (await context.Products.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Ensure categories exist
            if (!await context.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category { CategoryName = "Smartphones" },
                    new Category { CategoryName = "Tablets" },
                    new Category { CategoryName = "Accessories" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Get the first category
            var smartphoneCategory = await context.Categories.FirstOrDefaultAsync();
            if (smartphoneCategory == null) return;

            // Create sample products
            var products = new[]
            {
                new Product
                {
                    ProductName = "iPhone 15 Pro",
                    ShortDescription = "Latest iPhone with A17 Pro chip",
                    DetailDescription = "The iPhone 15 Pro features the powerful A17 Pro chip, titanium design, and advanced camera system.",
                    Price = 999.00m,
                    Stock = 50,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                },
                new Product
                {
                    ProductName = "Samsung Galaxy S24",
                    ShortDescription = "Flagship Android phone with AI features",
                    DetailDescription = "Samsung Galaxy S24 with advanced AI capabilities, stunning display, and professional camera.",
                    Price = 899.00m,
                    Stock = 30,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                },
                new Product
                {
                    ProductName = "Google Pixel 8",
                    ShortDescription = "Pure Android experience with AI photography",
                    DetailDescription = "Google Pixel 8 delivers the purest Android experience with cutting-edge AI photography features.",
                    Price = 699.00m,
                    Stock = 25,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                },
                new Product
                {
                    ProductName = "OnePlus 12",
                    ShortDescription = "Fast performance with premium design",
                    DetailDescription = "OnePlus 12 combines flagship performance with elegant design and fast charging technology.",
                    Price = 799.00m,
                    Stock = 20,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                },
                new Product
                {
                    ProductName = "Xiaomi 14 Pro",
                    ShortDescription = "High-end features at competitive price",
                    DetailDescription = "Xiaomi 14 Pro offers premium features including Leica cameras and fast wireless charging.",
                    Price = 649.00m,
                    Stock = 15,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            Console.WriteLine($"Database seeded with {products.Length} products");
        }
    }
}
