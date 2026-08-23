using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhoneStore.Attributes;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly PhoneStoreContext _context;
        private readonly ILogger<OrderController> _logger;
        private const int PageSize = 10;

        public OrderController(PhoneStoreContext context, ILogger<OrderController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));        }

        [AdminAuthorize(area: "Order", action: "Index")]
        public async Task<IActionResult> Index(string searchString, string status, string sortOrder, int? pageNumber, int? month, int? filterDays, string startDate, string endDate)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParam"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["AmountSortParam"] = sortOrder == "amount" ? "amount_desc" : "amount";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentStatus"] = status;

            var orders = from o in _context.Orders
                         .Include(o => o.Customer)
                         .Include(o => o.ShippingAddress)
                         select o;

            // Bộ lọc nâng cao (F2)
            if (month.HasValue)
            {
                orders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == month.Value && o.OrderDate.Value.Year == DateTime.Now.Year);
            }
            if (filterDays.HasValue)
            {
                var pastDate = DateTime.Now.AddDays(-filterDays.Value);
                orders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= pastDate);
            }
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParseExact(startDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime sd))
            {
                orders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= sd);
            }
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParseExact(endDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime ed))
            {
                ed = ed.AddDays(1).AddTicks(-1);
                orders = orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value <= ed);
            }

            // Tìm kiếm (Hỗ trợ tìm theo tên, sđt, mã đơn và THÁNG nếu nhập số 1-12)
            if (!string.IsNullOrEmpty(searchString))
            {
                bool isMonthSearch = int.TryParse(searchString, out int mValue) && mValue >= 1 && mValue <= 12;

                orders = orders.Where(o =>
                    (o.Customer != null && o.Customer.Name != null && o.Customer.Name.Contains(searchString)) ||
                    (o.Customer != null && o.Customer.Phone != null && o.Customer.Phone.Contains(searchString)) ||
                    (o.ShippingAddress != null && o.ShippingAddress.RecipientName != null && o.ShippingAddress.RecipientName.Contains(searchString)) ||
                    (o.OrderId.ToString() == searchString) ||
                    (isMonthSearch && o.OrderDate.HasValue && o.OrderDate.Value.Month == mValue));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "Tất cả")
            {
                orders = orders.Where(o => o.Status == status);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "date_desc":
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
                case "amount":
                    orders = orders.OrderBy(o => o.TotalAmount);
                    break;
                case "amount_desc":
                    orders = orders.OrderByDescending(o => o.TotalAmount);
                    break;
                default:
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
            }

            // Chuẩn bị danh sách trạng thái cho dropdown
            var statusList = new List<string>() { "Tất cả" };
            statusList.AddRange(Order.OrderStatus.AllStatuses);
            ViewBag.StatusList = new SelectList(statusList);

            // Thống kê số lượng đơn hàng theo trạng thái
            var orderStatusStats = await _context.Orders
                .GroupBy(o => o.Status ?? "Không xác định")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            ViewBag.OrderStatusStats = orderStatusStats;
            ViewBag.TotalOrders = await orders.CountAsync();

            return View(await PaginatedList<Order>.CreateAsync(orders, pageNumber ?? 1, PageSize));        }

        // GET: Order/Details/5
        [AdminAuthorize(area: "Order", action: "Index")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Coupon)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Color)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses, order.Status);

            return View(order);
        }

        // POST: Order/ChangeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            try
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã cập nhật trạng thái đơn hàng thành '{status}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn hàng");
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái đơn hàng" });
            }
        }

        // POST: Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            try
            {
                // Xóa chi tiết đơn hàng trước
                _context.OrderDetails.RemoveRange(order.OrderDetails);
                // Sau đó xóa đơn hàng
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đơn hàng đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa đơn hàng");
                return Json(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message });
            }
        }

        // GET: Order/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Coupon)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Color)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses, order.Status);
            ViewBag.PaymentMethods = new SelectList(new[] { "Tiền mặt khi nhận hàng", "Chuyển khoản ngân hàng", "Ví điện tử" }, order.PaymentMethod);

            return View(order);
        }

        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(area: "Order", action: "Edit")]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,Status,PaymentMethod,Notes")] Order orderUpdate)
        {
            if (id != orderUpdate.OrderId)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                order.Status = orderUpdate.Status;
                order.PaymentMethod = orderUpdate.PaymentMethod;
                order.Notes = orderUpdate.Notes;

                _context.Update(order);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Đơn hàng đã được cập nhật thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(orderUpdate.OrderId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }        // GET: Order/Create
        [AdminAuthorize(area: "Order", action: "Create")]
        public async Task<IActionResult> Create()
        {
            // Chuẩn bị dữ liệu cho dropdown
            ViewBag.Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "Name");
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");
            ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses);
            ViewBag.PaymentMethods = new SelectList(new[] { "Tiền mặt khi nhận hàng", "Chuyển khoản ngân hàng", "Ví điện tử" });
            
            return View();
        }        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(area: "Order", action: "Create")]
        public async Task<IActionResult> Create(Order order, int[] productIds, int[] quantities, int[] colorIds)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "Name", order.CustomerId);
                ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");
                ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses, order.Status);
                ViewBag.PaymentMethods = new SelectList(new[] { "Tiền mặt khi nhận hàng", "Chuyển khoản ngân hàng", "Ví điện tử" }, order.PaymentMethod);
                return View(order);
            }

            // Kiểm tra dữ liệu đầu vào
            if (productIds.Length == 0 || productIds.Length != quantities.Length)
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một sản phẩm vào đơn hàng");
                ViewBag.Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "Name", order.CustomerId);
                ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");
                ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses, order.Status);
                ViewBag.PaymentMethods = new SelectList(new[] { "Tiền mặt khi nhận hàng", "Chuyển khoản ngân hàng", "Ví điện tử" }, order.PaymentMethod);
                return View(order);
            }

            // Tạo transaction để đảm bảo tính nhất quán
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Thiết lập các giá trị mặc định
                order.OrderDate = DateTime.Now;
                
                if (string.IsNullOrEmpty(order.Status))
                {
                    order.Status = Order.OrderStatus.Processing;
                }

                // Lưu order trước để có OrderId
                _context.Add(order);
                await _context.SaveChangesAsync();

                // Tính tổng tiền và tạo chi tiết đơn hàng
                decimal totalAmount = 0;
                var orderDetails = new List<OrderDetail>();

                for (int i = 0; i < productIds.Length; i++)
                {
                    if (productIds[i] <= 0 || quantities[i] <= 0)
                        continue;

                    var product = await _context.Products
                        .Include(p => p.Discount)
                        .FirstOrDefaultAsync(p => p.ProductId == productIds[i]);

                    if (product == null)
                        continue;

                    // Kiểm tra tồn kho
                    if (product.Stock < quantities[i])
                    {
                        ModelState.AddModelError("", $"Sản phẩm '{product.ProductName}' chỉ còn {product.Stock} trong kho");
                        await transaction.RollbackAsync();
                        
                        ViewBag.Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "Name", order.CustomerId);
                        ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");
                        ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses, order.Status);
                        ViewBag.PaymentMethods = new SelectList(new[] { "Tiền mặt khi nhận hàng", "Chuyển khoản ngân hàng", "Ví điện tử" }, order.PaymentMethod);
                        return View(order);
                    }                    int? colorId = (i < colorIds.Length) ? (int?)colorIds[i] : null;

                    // Tính giá bán (có thể áp dụng giảm giá)
                    decimal price = product.Price;
                    if (product.Discount != null && product.Discount.DiscountPercent.HasValue && product.Discount.DiscountPercent.Value > 0)
                    {
                        price = price * (100 - product.Discount.DiscountPercent.Value) / 100;
                    }

                    var subTotal = price * quantities[i];
                    totalAmount += subTotal;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = product.ProductId,
                        ColorId = colorId,
                        Quantity = quantities[i]
                    };

                    orderDetails.Add(orderDetail);

                    // Cập nhật tồn kho
                    product.Stock -= quantities[i];
                    _context.Update(product);
                }

                // Lưu chi tiết đơn hàng
                await _context.OrderDetails.AddRangeAsync(orderDetails);
                
                // Cập nhật tổng tiền
                order.TotalAmount = totalAmount;
                _context.Update(order);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đơn hàng đã được tạo thành công.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng");
                ModelState.AddModelError("", $"Lỗi khi tạo đơn hàng: {ex.Message}");

                ViewBag.Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "Name", order.CustomerId);
                ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");
                ViewBag.StatusList = new SelectList(Order.OrderStatus.AllStatuses, order.Status);
                ViewBag.PaymentMethods = new SelectList(new[] { "Tiền mặt khi nhận hàng", "Chuyển khoản ngân hàng", "Ví điện tử" }, order.PaymentMethod);
                return View(order);
            }
        }
        
        // GET: Order/GetCustomerAddresses
        [HttpGet]
        public async Task<IActionResult> GetCustomerAddresses(int customerId)
        {
            var addresses = await _context.ShippingAddresses
                .Where(a => a.CustomerId == customerId)
                .Select(a => new 
                { 
                    a.AddressId, 
                    FullAddress = $"{a.RecipientName}, {a.Phone}, {a.AddressLine}, {a.Ward}, {a.District}, {a.Province}" 
                })
                .ToListAsync();
            
            return Json(addresses);
        }

        // GET: Order/GetProductColors
        [HttpGet]
        public async Task<IActionResult> GetProductColors(int productId)
        {
            var colors = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .Select(pi => pi.ColorId)
                .Distinct()
                .Join(_context.Colors,
                    colorId => colorId,
                    color => color.ColorId,
                    (colorId, color) => new { color.ColorId, color.ColorName })
                .ToListAsync();
            
            return Json(colors);
        }

        // GET: Order/GetProductDetails
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Discount)
                .Where(p => p.ProductId == productId)
                .Select(p => new 
                { 
                    p.ProductId, 
                    p.ProductName, 
                    p.Price, 
                    p.Stock,
                    DiscountPercent = p.Discount != null ? p.Discount.DiscountPercent : 0,
                    FinalPrice = p.Discount != null && p.Discount.DiscountPercent.HasValue ? 
                        p.Price * (100 - p.Discount.DiscountPercent.Value) / 100 : p.Price
                })
                .FirstOrDefaultAsync();
            
            if (product == null)
            {
                return NotFound();
            }
            
            
            return Json(product);
        }

        [AdminAuthorize(area: "Order", action: "Index")]
        public IActionResult ExportPdf()
        {
            var content = "Báo cáo Đơn hàng (Testing Export PDF Document).";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, "application/pdf", $"Order_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
    }
}
