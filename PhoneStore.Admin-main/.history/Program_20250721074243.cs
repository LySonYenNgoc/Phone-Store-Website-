using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using PhoneStore.Models;
using PhoneStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var arguments = args; // Save command line arguments


var builder = WebApplication.CreateBuilder(arguments);

// Configure request size limits for image uploads
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 20 * 1024 * 1024; // 20MB
});

// 🟢 Thêm dòng này để đăng ký DbContext
builder.Services.AddDbContext<PhoneStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PhoneStoreConnection")));

builder.Services.AddControllersWithViews();

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/AdminAccount/Login";
        options.LogoutPath = "/AdminAccount/Logout";
        options.AccessDeniedPath = "/AdminAccount/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
}); // ✅ Thêm session TRƯỚC khi build

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20MB
});

builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<PermissionSeedService>();

// Configure Data Protection
builder.Services.AddDataProtection()
    .SetApplicationName("PhoneStore");

// Cấu hình Anti-Forgery
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = false;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection(); // Chỉ redirect HTTPS trong production
}
else
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Session must come after UseRouting

app.UseAuthentication(); // Authentication before authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
