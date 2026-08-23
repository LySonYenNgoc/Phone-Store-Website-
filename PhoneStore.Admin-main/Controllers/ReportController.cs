using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Attributes;
using PhoneStore.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.Drawing;

namespace PhoneStore.Controllers
{
    [AdminAuthorize]
    public class ReportController : Controller
    {
        private readonly PhoneStoreContext _context;

        public ReportController(PhoneStoreContext context)
        {
            _context = context;
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // GET: Report/Revenue
        public async Task<IActionResult> Revenue(int? categoryId = null, string period = "")
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);
            var thirtyDaysAgo = today.AddDays(-30);

            // Lọc dữ liệu theo thời gian (preset)
            DateTime? filterStartDate = null;
            if (period.ToLower() == "today") filterStartDate = today;
            else if (period.ToLower() == "week") filterStartDate = today.AddDays(-(int)today.DayOfWeek);
            else if (period.ToLower() == "month") filterStartDate = startOfMonth;
            else if (period.ToLower() == "year") filterStartDate = startOfYear;

            // Lọc dữ liệu theo danh mục và thời gian
            var ordersQuery = _context.Orders.AsQueryable();
            var orderDetailsQuery = _context.OrderDetails.AsQueryable();

            if (categoryId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDetails.Any(od => od.Product != null && od.Product.CategoryId == categoryId.Value));
                orderDetailsQuery = orderDetailsQuery.Where(od => od.Product != null && od.Product.CategoryId == categoryId.Value);
            }
            
            if (filterStartDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= filterStartDate.Value);
                orderDetailsQuery = orderDetailsQuery.Where(od => od.OrderId != null && _context.Orders.Any(o => o.OrderId == od.OrderId && o.OrderDate >= filterStartDate.Value));
            }

            // Thống kê tổng quan
            var totalRevenue = await ordersQuery
                .SumAsync(o => o.TotalAmount ?? 0);

            var monthlyRevenue = await ordersQuery
                .Where(o => o.OrderDate >= startOfMonth)
                .SumAsync(o => o.TotalAmount ?? 0);

            var yearlyRevenue = await ordersQuery
                .Where(o => o.OrderDate >= startOfYear)
                .SumAsync(o => o.TotalAmount ?? 0);

            var last30DaysRevenue = await ordersQuery
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .SumAsync(o => o.TotalAmount ?? 0);

            // Thống kê đơn hàng
            var totalOrders = await ordersQuery.CountAsync();
            var monthlyOrders = await ordersQuery
                .Where(o => o.OrderDate >= startOfMonth)
                .CountAsync();

            var yearlyOrders = await ordersQuery
                .Where(o => o.OrderDate >= startOfYear)
                .CountAsync();

            // Doanh thu theo tháng (12 tháng gần nhất)
            var twelveMonthsAgo = DateTime.Now.AddMonths(-11);
            var monthlyData = await ordersQuery
                .Where(o => o.OrderDate >= twelveMonthsAgo)
                .GroupBy(o => new {
                    Year = o.OrderDate!.Value.Year,
                    Month = o.OrderDate.Value.Month
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount ?? 0)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Doanh thu theo ngày (30 ngày gần nhất)
            var dailyData = await ordersQuery
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .GroupBy(o => o.OrderDate!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();            // Top sản phẩm bán chạy
            var topProducts = await orderDetailsQuery
                .Include(od => od.Product)
                .GroupBy(od => new { od.ProductId, od.Product!.ProductName })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(od => od.Quantity ?? 0),
                    TotalRevenue = g.Sum(od => (od.Quantity ?? 0) * (od.Product!.Price))
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync();            // Doanh thu theo danh mục
            var categoryRevenue = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p!.Category)
                .GroupBy(od => new {
                    CategoryId = od.Product!.CategoryId,
                    CategoryName = od.Product.Category!.CategoryName
                })
                .Select(g => new
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalRevenue = g.Sum(od => (od.Quantity ?? 0) * (od.Product!.Price)),
                    TotalQuantity = g.Sum(od => od.Quantity ?? 0)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            // Thống kê khách hàng
            var customerStats = await ordersQuery
                .Include(o => o.Customer)
                .GroupBy(o => new {
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer!.Name
                })
                .Select(g => new
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    TotalSpent = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount ?? 0)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho biểu đồ
            var chartMonths = new List<string>();
            var chartRevenue = new List<decimal>();
            var chartOrders = new List<int>();

            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                chartMonths.Add(date.ToString("MM/yyyy"));

                var monthData = monthlyData.FirstOrDefault(d =>
                    d.Year == date.Year && d.Month == date.Month);

                chartRevenue.Add(monthData?.Revenue ?? 0);
                chartOrders.Add(monthData?.OrderCount ?? 0);
            }

            // Dữ liệu biểu đồ ngày
            var chartDays = new List<string>();
            var chartDailyRevenue = new List<decimal>();

            for (int i = 29; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                chartDays.Add(date.ToString("dd/MM"));

                var dayData = dailyData.FirstOrDefault(d => d.Date == date);
                chartDailyRevenue.Add(dayData?.Revenue ?? 0);
            }

            // Truyền dữ liệu vào ViewBag
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.YearlyRevenue = yearlyRevenue;
            ViewBag.Last30DaysRevenue = last30DaysRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.MonthlyOrders = monthlyOrders;
            ViewBag.YearlyOrders = yearlyOrders;
            ViewBag.AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            ViewBag.ChartMonths = string.Join(",", chartMonths.Select(m => $"'{m}'"));
            ViewBag.ChartRevenue = string.Join(",", chartRevenue);
            ViewBag.ChartOrders = string.Join(",", chartOrders);
            ViewBag.ChartDays = string.Join(",", chartDays.Select(d => $"'{d}'"));
            ViewBag.ChartDailyRevenue = string.Join(",", chartDailyRevenue);

            ViewBag.TopProducts = topProducts;
            ViewBag.CategoryRevenue = categoryRevenue;
            ViewBag.CustomerStats = customerStats;
            ViewBag.MonthlyData = monthlyData;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedPeriod = period;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View();
        }

        // GET: Report/ExportRevenue
        public async Task<IActionResult> ExportRevenue(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Thiết lập ngày mặc định nếu không có
            startDate ??= DateTime.Today.AddMonths(-12);
            endDate ??= DateTime.Today;

            // Lấy dữ liệu đơn hàng trong khoảng thời gian
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p!.Category)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Tạo Excel package
            using var package = new ExcelPackage();

            // Trang tổng quan
            var summarySheet = package.Workbook.Worksheets.Add("Tổng quan");
            CreateSummarySheet(summarySheet, orders, startDate.Value, endDate.Value);

            // Trang chi tiết đơn hàng
            var ordersSheet = package.Workbook.Worksheets.Add("Chi tiết đơn hàng");
            CreateOrdersSheet(ordersSheet, orders);

            // Trang thống kê sản phẩm
            var productsSheet = package.Workbook.Worksheets.Add("Thống kê sản phẩm");
            CreateProductsSheet(productsSheet, orders);

            // Trang thống kê khách hàng
            var customersSheet = package.Workbook.Worksheets.Add("Thống kê khách hàng");
            CreateCustomersSheet(customersSheet, orders);

            // Trang thống kê theo tháng
            var monthlySheet = package.Workbook.Worksheets.Add("Thống kê theo tháng");
            CreateMonthlySheet(monthlySheet, orders);

            // Xuất file
            var fileName = $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
            var fileBytes = package.GetAsByteArray();

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private void CreateSummarySheet(ExcelWorksheet sheet, List<Order> orders, DateTime startDate, DateTime endDate)
        {
            sheet.Cells["A1"].Value = "BÁO CÁO DOANH THU";
            sheet.Cells["A1:F1"].Merge = true;
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            sheet.Cells["A2"].Value = $"Từ ngày: {startDate:dd/MM/yyyy} - Đến ngày: {endDate:dd/MM/yyyy}";
            sheet.Cells["A2:F2"].Merge = true;
            sheet.Cells["A2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Thống kê tổng quan
            var totalRevenue = orders.Sum(o => o.TotalAmount ?? 0);
            var totalOrders = orders.Count;
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            sheet.Cells["A4"].Value = "THỐNG KÊ TỔNG QUAN";
            sheet.Cells["A4"].Style.Font.Bold = true;

            sheet.Cells["A5"].Value = "Tổng doanh thu:";
            sheet.Cells["B5"].Value = totalRevenue;
            sheet.Cells["B5"].Style.Numberformat.Format = "#,##0 \"VNĐ\"";

            sheet.Cells["A6"].Value = "Tổng số đơn hàng:";
            sheet.Cells["B6"].Value = totalOrders;

            sheet.Cells["A7"].Value = "Giá trị đơn hàng trung bình:";
            sheet.Cells["B7"].Value = averageOrderValue;
            sheet.Cells["B7"].Style.Numberformat.Format = "#,##0 \"VNĐ\"";

            // Thống kê theo trạng thái
            var statusStats = orders.GroupBy(o => o.Status ?? "Không xác định")
                .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount ?? 0) })
                .ToList();

            sheet.Cells["A9"].Value = "THỐNG KÊ THEO TRẠNG THÁI";
            sheet.Cells["A9"].Style.Font.Bold = true;

            sheet.Cells["A10"].Value = "Trạng thái";
            sheet.Cells["B10"].Value = "Số đơn";
            sheet.Cells["C10"].Value = "Doanh thu";
            sheet.Cells["A10:C10"].Style.Font.Bold = true;

            int row = 11;
            foreach (var stat in statusStats)
            {
                sheet.Cells[row, 1].Value = stat.Status;
                sheet.Cells[row, 2].Value = stat.Count;
                sheet.Cells[row, 3].Value = stat.Revenue;
                sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateOrdersSheet(ExcelWorksheet sheet, List<Order> orders)
        {
            sheet.Cells["A1"].Value = "CHI TIẾT ĐƠN HÀNG";
            sheet.Cells["A1"].Style.Font.Bold = true;

            // Header
            string[] headers = { "Mã đơn hàng", "Ngày đặt", "Khách hàng", "Tổng tiền", "Trạng thái", "Số sản phẩm" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[2, i + 1].Value = headers[i];
                sheet.Cells[2, i + 1].Style.Font.Bold = true;
            }

            // Data
            int row = 3;
            foreach (var order in orders)
            {
                sheet.Cells[row, 1].Value = order.OrderId;
                sheet.Cells[row, 2].Value = order.OrderDate?.ToString("dd/MM/yyyy HH:mm");
                sheet.Cells[row, 3].Value = order.Customer?.Name ?? "N/A";
                sheet.Cells[row, 4].Value = order.TotalAmount ?? 0;
                sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                sheet.Cells[row, 5].Value = order.Status ?? "N/A";
                sheet.Cells[row, 6].Value = order.OrderDetails?.Sum(od => od.Quantity) ?? 0;
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateProductsSheet(ExcelWorksheet sheet, List<Order> orders)
        {
            sheet.Cells["A1"].Value = "THỐNG KÊ SẢN PHẨM";
            sheet.Cells["A1"].Style.Font.Bold = true;

            var productStats = orders                .SelectMany(o => o.OrderDetails!)
                .GroupBy(od => new { od.ProductId, od.Product!.ProductName })
                .Select(g => new
                {
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(od => od.Quantity ?? 0),
                    TotalRevenue = g.Sum(od => (od.Quantity ?? 0) * (od.Product!.Price)),
                    OrderCount = g.Select(od => od.OrderId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            // Header
            string[] headers = { "Tên sản phẩm", "Số lượng bán", "Doanh thu", "Số đơn hàng" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[2, i + 1].Value = headers[i];
                sheet.Cells[2, i + 1].Style.Font.Bold = true;
            }

            // Data
            int row = 3;
            foreach (var product in productStats)
            {
                sheet.Cells[row, 1].Value = product.ProductName;
                sheet.Cells[row, 2].Value = product.TotalQuantity;
                sheet.Cells[row, 3].Value = product.TotalRevenue;
                sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                sheet.Cells[row, 4].Value = product.OrderCount;
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateCustomersSheet(ExcelWorksheet sheet, List<Order> orders)
        {
            sheet.Cells["A1"].Value = "THỐNG KÊ KHÁCH HÀNG";
            sheet.Cells["A1"].Style.Font.Bold = true;

            var customerStats = orders
                .GroupBy(o => new { o.CustomerId, o.Customer!.Name })
                .Select(g => new
                {
                    CustomerName = g.Key.Name,
                    TotalSpent = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount ?? 0),
                    FirstOrder = g.Min(o => o.OrderDate),
                    LastOrder = g.Max(o => o.OrderDate)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();

            // Header
            string[] headers = { "Tên khách hàng", "Tổng chi tiêu", "Số đơn hàng", "Giá trị TB/đơn", "Đơn đầu tiên", "Đơn cuối cùng" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[2, i + 1].Value = headers[i];
                sheet.Cells[2, i + 1].Style.Font.Bold = true;
            }

            // Data
            int row = 3;
            foreach (var customer in customerStats)
            {
                sheet.Cells[row, 1].Value = customer.CustomerName;
                sheet.Cells[row, 2].Value = customer.TotalSpent;
                sheet.Cells[row, 2].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                sheet.Cells[row, 3].Value = customer.OrderCount;
                sheet.Cells[row, 4].Value = customer.AverageOrderValue;
                sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                sheet.Cells[row, 5].Value = customer.FirstOrder?.ToString("dd/MM/yyyy");
                sheet.Cells[row, 6].Value = customer.LastOrder?.ToString("dd/MM/yyyy");
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateMonthlySheet(ExcelWorksheet sheet, List<Order> orders)
        {
            sheet.Cells["A1"].Value = "THỐNG KÊ THEO THÁNG";
            sheet.Cells["A1"].Style.Font.Bold = true;

            var monthlyStats = orders
                .GroupBy(o => new {
                    Year = o.OrderDate!.Value.Year,
                    Month = o.OrderDate.Value.Month
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount ?? 0)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            // Header
            string[] headers = { "Tháng/Năm", "Doanh thu", "Số đơn hàng", "Giá trị TB/đơn" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[2, i + 1].Value = headers[i];
                sheet.Cells[2, i + 1].Style.Font.Bold = true;
            }

            // Data
            int row = 3;
            foreach (var month in monthlyStats)
            {
                sheet.Cells[row, 1].Value = $"{month.Month:00}/{month.Year}";
                sheet.Cells[row, 2].Value = month.Revenue;
                sheet.Cells[row, 2].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                sheet.Cells[row, 3].Value = month.OrderCount;
                sheet.Cells[row, 4].Value = month.AverageOrderValue;
                sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }
    }
}
