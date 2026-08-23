using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure culture settings for consistent number formatting
var defaultCulture = builder.Configuration.GetValue<string>("CultureSettings:DefaultCulture") ?? "vi-VN";
var supportedCultures = builder.Configuration.GetSection("CultureSettings:SupportedCultures").Get<string[]>() ?? new[] { "vi-VN", "en-US" };

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(defaultCulture)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Set default culture for the application
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(defaultCulture);
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(defaultCulture);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add anti-forgery services
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Add Entity Framework
builder.Services.AddDbContext<PhoneStore.Customer.Models.PhoneStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PhoneStore.Customer.Models.PhoneStoreContext>();

    // Check if products already exist
    if (!await context.Products.AnyAsync())
    {
        // Ensure categories exist
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new PhoneStore.Customer.Models.Category { CategoryName = "Smartphones" },
                new PhoneStore.Customer.Models.Category { CategoryName = "Tablets" },
                new PhoneStore.Customer.Models.Category { CategoryName = "Accessories" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // Get the first category
        var smartphoneCategory = await context.Categories.FirstOrDefaultAsync();
        if (smartphoneCategory != null)
        {
            // Create sample products
            var products = new[]
            {
                new PhoneStore.Customer.Models.Product
                {
                    ProductName = "iPhone 15 Pro",
                    ShortDescription = "Latest iPhone with A17 Pro chip",
                    DetailDescription = "The iPhone 15 Pro features the powerful A17 Pro chip, titanium design, and advanced camera system.",
                    Price = 999.00m,
                    Stock = 50,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                },
                new PhoneStore.Customer.Models.Product
                {
                    ProductName = "Samsung Galaxy S24",
                    ShortDescription = "Flagship Android phone with AI features",
                    DetailDescription = "Samsung Galaxy S24 with advanced AI capabilities, stunning display, and professional camera.",
                    Price = 899.00m,
                    Stock = 30,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                },
                new PhoneStore.Customer.Models.Product
                {
                    ProductName = "Google Pixel 8",
                    ShortDescription = "Pure Android experience with AI photography",
                    DetailDescription = "Google Pixel 8 delivers the purest Android experience with cutting-edge AI photography features.",
                    Price = 699.00m,
                    Stock = 25,
                    CategoryId = smartphoneCategory.CategoryId,
                    IsPublished = true
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            Console.WriteLine($"Database seeded with {products.Length} products");
        }
    }

    // Ensure CartItems table exists
    await context.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CartItems' AND xtype='U')
        BEGIN
            CREATE TABLE CartItems (
                CartItemId INT PRIMARY KEY IDENTITY(1,1),
                CustomerId INT NOT NULL,
                ProductId INT NOT NULL,
                Quantity INT NOT NULL,
                ColorId INT NULL,
                DateAdded DATETIME NOT NULL DEFAULT GETDATE(),
                CONSTRAINT FK_CartItems_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerID) ON DELETE CASCADE,
                CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE
            )
        END");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Configure localization for consistent number formatting
app.UseRequestLocalization(localizationOptions);

app.UseSession();

// Custom middleware to check remember me cookies
app.Use(async (context, next) =>
{
    // Check if user is not logged in but has remember me cookies
    if (context.Session.GetInt32("CustomerId") == null)
    {
        if (context.Request.Cookies.TryGetValue("RememberMe_CustomerId", out var customerIdStr) &&
            context.Request.Cookies.TryGetValue("RememberMe_CustomerName", out var customerName) &&
            int.TryParse(customerIdStr, out var customerId))
        {
            // Restore session from cookies
            context.Session.SetInt32("CustomerId", customerId);
            context.Session.SetString("CustomerName", customerName);
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
