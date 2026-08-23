using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Attributes;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    public class DashboardController : Controller
    {
        private readonly PhoneStoreContext _context;

        public DashboardController(PhoneStoreContext context)
        {
            _context = context;
        }        // Action để tạo dữ liệu đơn hàng giả        [HttpGet]
        [AdminAuthorize(area: "Dashboard", action: "GenerateFakeOrders")]
        public async Task<IActionResult> GenerateFakeOrders()
        {
            try
            {
                // Kiểm tra xem đã có đơn hàng chưa
                var orderCount = await _context.Orders.CountAsync();
                if (orderCount > 10)
                {
                    TempData["Message"] = "Đã có nhiều đơn hàng trong hệ thống. Không cần tạo thêm dữ liệu giả.";
                    return RedirectToAction("Index");
                }                // Đếm số lượng sản phẩm
                var totalProductCount = await _context.Products.CountAsync();
                
                // Lấy danh sách khách hàng
                var customers = await _context.Customers.ToListAsync();
                if (customers.Count == 0)
                {
                    // Nếu chưa có khách hàng, tạo một số khách hàng mẫu
                    customers = CreateSampleCustomers();
                    await _context.Customers.AddRangeAsync(customers);
                    await _context.SaveChangesAsync();
                    
                    TempData["Info"] = $"Đã tạo {customers.Count} khách hàng mẫu.";
                }                // Lấy danh sách sản phẩm
                var products = await _context.Products.ToListAsync();
                if (products.Count == 0)
                {
                    // Kiểm tra danh mục
                    var categories = await _context.Categories.ToListAsync();
                    if (categories.Count == 0)
                    {
                        // Tạo danh mục mẫu
                        categories = CreateSampleCategories();
                        await _context.Categories.AddRangeAsync(categories);
                        await _context.SaveChangesAsync();
                        TempData["Info"] = (TempData["Info"] ?? "") + " Đã tạo " + categories.Count + " danh mục mẫu.";
                    }
                    
                    // Nếu chưa có sản phẩm, tạo một số sản phẩm mẫu
                    products = CreateSampleProducts();
                    await _context.Products.AddRangeAsync(products);
                    await _context.SaveChangesAsync();
                    
                    TempData["Info"] = (TempData["Info"] ?? "") + " Đã tạo " + products.Count + " sản phẩm mẫu.";
                }

                // Tạo đơn hàng mẫu
                var random = new Random();
                var orders = new List<Order>();
                
                // Tạo đơn hàng cho 6 tháng gần đây (tính từ ngày hiện tại là 3/6/2025)
                DateTime currentDate = new DateTime(2025, 6, 3);
                
                for (int i = 0; i < 6; i++)
                {
                    var month = currentDate.AddMonths(-i);
                    var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
                    
                    // Tạo 5-15 đơn hàng mỗi tháng
                    var orderCountForMonth = random.Next(5, 16);
                    
                    for (int j = 0; j < orderCountForMonth; j++)
                    {
                        // Chọn ngày ngẫu nhiên trong tháng
                        var day = random.Next(1, daysInMonth + 1);
                        var orderDate = new DateTime(month.Year, month.Month, day);
                        if (orderDate > currentDate)
                        {
                            // Đảm bảo ngày đặt hàng không vượt quá ngày hiện tại
                            orderDate = currentDate;
                        }
                        
                        // Chọn khách hàng ngẫu nhiên
                        var customer = customers[random.Next(customers.Count)];
                        
                        // Tạo đơn hàng
                        var order = new Order
                        {
                            CustomerId = customer.CustomerId,
                            OrderDate = orderDate,
                        };
                        
                        // Thêm 1-5 sản phẩm vào đơn hàng
                        var productCount = random.Next(1, 6);
                        var selectedProductIds = new HashSet<int>();
                        decimal totalAmount = 0;
                        var orderDetails = new List<OrderDetail>();
                        
                        for (int k = 0; k < productCount; k++)
                        {
                            // Chọn sản phẩm ngẫu nhiên (không trùng lặp)
                            var product = products[random.Next(products.Count)];
                            if (selectedProductIds.Contains(product.ProductId))
                            {
                                // Nếu sản phẩm đã được chọn, thử chọn lại
                                if (k > 0) k--;
                                continue;
                            }
                            
                            selectedProductIds.Add(product.ProductId);
                            
                            // Số lượng từ 1-3
                            var quantity = random.Next(1, 4);
                            
                            // Tính giá
                            var price = product.Price;
                            var subtotal = price * quantity;
                            totalAmount += subtotal;
                            
                            // Thêm chi tiết đơn hàng
                            var orderDetail = new OrderDetail
                            {
                                ProductId = product.ProductId,
                                Quantity = quantity
                            };
                            
                            orderDetails.Add(orderDetail);
                        }
                        
                        order.TotalAmount = totalAmount;
                          // Lưu đơn hàng và chi tiết đơn hàng trong một transaction để đảm bảo tính nhất quán
                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                await _context.Orders.AddAsync(order);
                                await _context.SaveChangesAsync();
                                
                                // Gán OrderId cho chi tiết đơn hàng và lưu
                                foreach (var detail in orderDetails)
                                {
                                    detail.OrderId = order.OrderId;
                                }
                                
                                await _context.OrderDetails.AddRangeAsync(orderDetails);
                                await _context.SaveChangesAsync();
                                
                                await transaction.CommitAsync();
                            }
                            catch
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                        
                        orders.Add(order);
                    }
                }
                
                TempData["Message"] = $"Đã tạo thành công {orders.Count} đơn hàng mẫu.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tạo đơn hàng mẫu: {ex.Message}";
            }
            
            return RedirectToAction("Index");
        }
        
        private List<Customer> CreateSampleCustomers()
        {
            var customers = new List<Customer>
            {
                new Customer { Name = "Nguyễn Văn An", Email = "an@example.com", Phone = "0901234567", BirthYear = 1990 },
                new Customer { Name = "Trần Thị Bình", Email = "binh@example.com", Phone = "0912345678", BirthYear = 1992 },
                new Customer { Name = "Lê Văn Cường", Email = "cuong@example.com", Phone = "0923456789", BirthYear = 1988 },
                new Customer { Name = "Phạm Thị Dung", Email = "dung@example.com", Phone = "0934567890", BirthYear = 1995 },
                new Customer { Name = "Hoàng Văn Em", Email = "em@example.com", Phone = "0945678901", BirthYear = 1991 }
            };
            
            return customers;
        }
        
        private List<Product> CreateSampleProducts()
        {
            var products = new List<Product>
            {
                new Product { 
                    ProductName = "iPhone 15 Pro", 
                    ShortDescription = "Điện thoại iPhone 15 Pro mới nhất", 
                    DetailDescription = "iPhone 15 Pro với chip A17 Pro, camera 48MP và thiết kế titan cao cấp",
                    Price = 29990000,
                    Stock = 50,
                    CategoryId = 1
                },
                new Product { 
                    ProductName = "Samsung Galaxy S24 Ultra", 
                    ShortDescription = "Flagship mới nhất của Samsung", 
                    DetailDescription = "Galaxy S24 Ultra với bút S-Pen, camera 200MP và Snapdragon 8 Gen 3",
                    Price = 31990000,
                    Stock = 40,
                    CategoryId = 1
                },
                new Product { 
                    ProductName = "Xiaomi 14 Pro", 
                    ShortDescription = "Điện thoại cao cấp của Xiaomi", 
                    DetailDescription = "Xiaomi 14 Pro với Snapdragon 8 Gen 3, camera Leica và sạc siêu nhanh 120W",
                    Price = 19990000,
                    Stock = 35,
                    CategoryId = 1
                },
                new Product { 
                    ProductName = "Google Pixel 8 Pro", 
                    ShortDescription = "Flagship của Google với AI tuyệt vời", 
                    DetailDescription = "Google Pixel 8 Pro với camera hàng đầu và các tính năng AI độc quyền",
                    Price = 22990000,
                    Stock = 25,
                    CategoryId = 1
                },
                new Product { 
                    ProductName = "OPPO Find X7 Ultra", 
                    ShortDescription = "Điện thoại chụp ảnh hàng đầu", 
                    DetailDescription = "OPPO Find X7 Ultra với hệ thống 4 camera Hasselblad 50MP và zoom quang học 6x",
                    Price = 24990000,
                    Stock = 30,
                    CategoryId = 1
                }
            };
            
            return products;
        }
          private List<Category> CreateSampleCategories()
        {
            var categories = new List<Category>
            {
                new Category { CategoryName = "Điện thoại" },
                new Category { CategoryName = "Tablet" },
                new Category { CategoryName = "Laptop" },
                new Category { CategoryName = "Phụ kiện" },
                new Category { CategoryName = "Smartwatch" }
            };
              return categories;
        }

        [AdminAuthorize(area: "Dashboard", action: "Index")]
        public async Task<IActionResult> Index(int? month, int? filterDays, string startDate, string endDate)
        {
            IQueryable<Order> orderQuery = _context.Orders;

            if (month.HasValue)
            {
                orderQuery = orderQuery.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == month.Value && o.OrderDate.Value.Year == DateTime.Now.Year);
            }
            if (filterDays.HasValue)
            {
                var pastDate = DateTime.Now.AddDays(-filterDays.Value);
                orderQuery = orderQuery.Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= pastDate);
            }
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParseExact(startDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime sd))
            {
                orderQuery = orderQuery.Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= sd);
            }
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParseExact(endDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime ed))
            {
                ed = ed.AddDays(1).AddTicks(-1);
                orderQuery = orderQuery.Where(o => o.OrderDate.HasValue && o.OrderDate.Value <= ed);
            }

            // Tổng số tài khoản chờ duyệt
            var pendingAdmins = await _context.Admins
                .Where(a => !a.IsApproved && !a.IsBlocked)
                .ToListAsync();

            // Tổng số người dùng
            var totalAdmins = await _context.Admins.CountAsync();
            var activeAdmins = await _context.Admins
                .Where(a => a.IsApproved && !a.IsBlocked)
                .CountAsync();

            // Tổng số khách hàng
            var totalCustomers = await _context.Customers.CountAsync();

            // Tổng số sản phẩm
            var totalProducts = await _context.Products.CountAsync();
            var lowStockProducts = await _context.Products
                .Where(p => p.Stock <= 5 && p.Stock > 0)
                .CountAsync();
            var outOfStockProducts = await _context.Products
                .Where(p => p.Stock == 0)
                .CountAsync();            // Thống kê đơn hàng - Sử dụng orderQuery đã được lọc
            var totalOrders = await orderQuery.CountAsync();
            var recentOrders = await orderQuery
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();
                
            // Thống kê đơn hàng theo trạng thái
            var orderStatusStats = await orderQuery
                .GroupBy(o => o.Status ?? "Không xác định")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
                
            // Đơn hàng mới trong 7 ngày qua
            var newOrdersLast7Days = await _context.Orders
                .Where(o => o.OrderDate != null && o.OrderDate >= DateTime.Now.AddDays(-7))
                .CountAsync();

            // Doanh thu theo tháng (6 tháng gần nhất)
            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate != null && o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { Month = o.OrderDate!.Value.Month, Year = o.OrderDate.Value.Year })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();
                
            // Thống kê doanh thu theo ngày (7 ngày gần nhất)
            var sevenDaysAgo = DateTime.Now.AddDays(-6);
            var dailyRevenue = await _context.Orders
                .Where(o => o.OrderDate != null && o.OrderDate >= sevenDaysAgo)
                .GroupBy(o => o.OrderDate!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            // Danh mục phổ biến nhất
            var popularCategories = await _context.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .Take(5)
                .ToListAsync();

            var categoryIds = popularCategories.Select(pc => pc.CategoryId).ToList();
            var categories = await _context.Categories
                .Where(c => categoryIds.Contains(c.CategoryId))
                .ToListAsync();

            var categoryData = popularCategories.Select(pc => new
            {
                CategoryName = categories.FirstOrDefault(c => c.CategoryId == pc.CategoryId)?.CategoryName ?? "Unknown",
                Count = pc.Count
            }).ToList();

            // Dữ liệu cho biểu đồ tháng
            var months = new List<string>();
            var revenueData = new List<decimal>();
            var orderCountData = new List<int>();
            
            // Lấy 6 tháng gần nhất
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var monthName = date.ToString("MM/yyyy");
                months.Add(monthName);
                
                var monthData = monthlyRevenue
                    .FirstOrDefault(r => r.Month == date.Month && r.Year == date.Year);
                    
                revenueData.Add(monthData?.Revenue ?? 0);
                orderCountData.Add(monthData?.OrderCount ?? 0);
            }
            
            // Dữ liệu cho biểu đồ ngày
            var days = new List<string>();
            var dailyRevenueData = new List<decimal>();
            var dailyOrderCountData = new List<int>();
            
            // Lấy 7 ngày gần nhất
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i);
                var dayName = date.ToString("dd/MM");
                days.Add(dayName);
                
                var dayData = dailyRevenue
                    .FirstOrDefault(r => r.Date.Day == date.Day && r.Date.Month == date.Month && r.Date.Year == date.Year);
                    
                dailyRevenueData.Add(dayData?.Revenue ?? 0);
                dailyOrderCountData.Add(dayData?.OrderCount ?? 0);
            }
            
            // Truyền dữ liệu vào ViewBag
            ViewBag.PendingAdmins = pendingAdmins;
            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.ActiveAdmins = activeAdmins;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.OutOfStockProducts = outOfStockProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.CategoryData = categoryData;
            ViewBag.OrderStatusStats = orderStatusStats;
            ViewBag.NewOrdersLast7Days = newOrdersLast7Days;
            ViewBag.ProcessingOrders = orderStatusStats.ContainsKey(Order.OrderStatus.Processing) ? orderStatusStats[Order.OrderStatus.Processing] : 0;
            
            // Dữ liệu biểu đồ tháng
            ViewBag.ChartMonths = string.Join(",", months.Select(m => $"'{m}'"));
            ViewBag.ChartRevenueData = string.Join(",", revenueData);
            ViewBag.ChartOrderCountData = string.Join(",", orderCountData);
            
            // Dữ liệu biểu đồ ngày
            ViewBag.ChartDays = string.Join(",", days.Select(d => $"'{d}'"));
            ViewBag.ChartDailyRevenueData = string.Join(",", dailyRevenueData);
            ViewBag.ChartDailyOrderCountData = string.Join(",", dailyOrderCountData);
            
            // Tính tổng doanh thu theo điều kiện lọc
            ViewBag.TotalRevenue = await orderQuery.SumAsync(o => o.TotalAmount ?? 0);
            
            // Thông kê sản phẩm bán chạy nhất
            var topSellingProducts = await _context.OrderDetails
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(od => od.Quantity ?? 0)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(5)
                .ToListAsync();
                
            var productIds = topSellingProducts.Select(p => p.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();
                
            var topProductsData = topSellingProducts.Select(tp => new
            {
                ProductName = products.FirstOrDefault(p => p.ProductId == tp.ProductId)?.ProductName ?? "Unknown",
                Quantity = tp.TotalQuantity
            }).ToList();
            
            ViewBag.TopProducts = topProductsData;

            return View();
        }

        [AdminAuthorize(area: "Dashboard", action: "ExportPdf")]
        public IActionResult ExportPdf()
        {
            var content = "Báo cáo Doanh thu Hệ thống (Testing Export PDF Document).";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, "application/pdf", $"Dashboard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
    }
}