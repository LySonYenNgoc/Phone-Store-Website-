using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Customer.Models;
using PhoneStore.Customer.ViewModels;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;

namespace PhoneStore.Customer.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly PhoneStoreContext _context;
        private const string CART_SESSION_KEY = "Cart";

        // Payment methods available in the system
        private readonly List<string> PaymentMethods = new()
        {
            "Tiền mặt khi nhận hàng",
            "Chuyển khoản ngân hàng",
            "Ví điện tử"
        };

        public CheckoutController(PhoneStoreContext context)
        {
            _context = context;
        }

        // GET: /checkout
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống";
                return RedirectToAction("Index", "Cart");
            }            // Check if user is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục thanh toán";
                return RedirectToAction("Login", "Customer", new { returnUrl = "/checkout" });
            }            // Get customer's shipping addresses
            var addresses = await _context.ShippingAddresses
                .Where(a => a.CustomerId == customerId.Value)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new ShippingAddressViewModel
                {
                    AddressId = a.AddressId,
                    RecipientName = a.RecipientName ?? "",
                    Phone = a.Phone ?? "",
                    AddressLine = a.AddressLine ?? "",
                    Ward = a.Ward ?? "",
                    District = a.District ?? "",
                    Province = a.Province ?? "",
                    IsDefault = a.IsDefault ?? false
                })
                .ToListAsync();

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart.Items.Select(i => new CartItemViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    ImageUrl = i.ImageUrl
                }).ToList(),
                Subtotal = cart.Total,
                Total = cart.Total, // For now, no shipping fees or discounts
                Addresses = addresses,
                PaymentMethods = PaymentMethods,
                NewAddress = new ShippingAddressViewModel()
            };

            return View(viewModel);
        }

        // POST: /checkout/placeorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống" });
            }            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục" });
            }

            // Validate required fields
            if (string.IsNullOrEmpty(model.SelectedPaymentMethod))
            {
                return Json(new { success = false, message = "Vui lòng chọn phương thức thanh toán" });
            }

            int shippingAddressId;

            // Handle shipping address
            if (model.CreateNewAddress && model.NewAddress != null)
            {
                // Validate new address
                if (string.IsNullOrWhiteSpace(model.NewAddress.RecipientName) ||
                    string.IsNullOrWhiteSpace(model.NewAddress.Phone) ||
                    string.IsNullOrWhiteSpace(model.NewAddress.AddressLine) ||
                    string.IsNullOrWhiteSpace(model.NewAddress.Ward) ||
                    string.IsNullOrWhiteSpace(model.NewAddress.District) ||
                    string.IsNullOrWhiteSpace(model.NewAddress.Province))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin địa chỉ mới" });
                }                // Create new shipping address
                var newAddress = new ShippingAddress
                {
                    CustomerId = customerId.Value,
                    RecipientName = model.NewAddress.RecipientName.Trim(),
                    Phone = model.NewAddress.Phone.Trim(),
                    AddressLine = model.NewAddress.AddressLine.Trim(),
                    Ward = model.NewAddress.Ward.Trim(),
                    District = model.NewAddress.District.Trim(),
                    Province = model.NewAddress.Province.Trim(),
                    IsDefault = model.NewAddress.IsDefault
                };

                // If this is set as default, remove default from other addresses
                if (newAddress.IsDefault == true)
                {                    var existingAddresses = await _context.ShippingAddresses
                        .Where(a => a.CustomerId == customerId.Value && a.IsDefault == true)
                        .ToListAsync();

                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                _context.ShippingAddresses.Add(newAddress);
                await _context.SaveChangesAsync();
                shippingAddressId = newAddress.AddressId;
            }
            else if (model.SelectedAddressId.HasValue)
            {                // Use existing address
                var existingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(a => a.AddressId == model.SelectedAddressId.Value && a.CustomerId == customerId.Value);

                if (existingAddress == null)
                {
                    return Json(new { success = false, message = "Địa chỉ giao hàng không hợp lệ" });
                }

                shippingAddressId = existingAddress.AddressId;
            }
            else
            {
                return Json(new { success = false, message = "Vui lòng chọn địa chỉ giao hàng" });
            }

            // Create order using transaction
            using var transaction = await _context.Database.BeginTransactionAsync();            try
            {                // Create order
                var order = new Order
                {
                    CustomerId = customerId.Value,
                    ShippingAddressId = shippingAddressId,
                    OrderDate = DateTime.Now,
                    TotalAmount = cart.Total,
                    Status = "Đang xử lý", // Initial status
                    PaymentMethod = model.SelectedPaymentMethod,
                    Notes = model.Notes?.Trim() ?? string.Empty
                };

                _context.Orders.Add(order);

                // First save to get the OrderId
                await _context.SaveChangesAsync();

                // Create order details and update product stock
                foreach (var cartItem in cart.Items)
                {                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == cartItem.ProductId);

                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Sản phẩm {cartItem.ProductName} không tồn tại" });
                    }

                    // Validate if product is published
                    if (!product.IsPublished)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Sản phẩm {cartItem.ProductName} hiện không có sẵn" });
                    }

                    // Check if product has enough stock
                    if (product.Stock < cartItem.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Sản phẩm {cartItem.ProductName} chỉ còn {product.Stock} sản phẩm trong kho" });
                    }// Calculate the total price for this item
                    decimal totalPrice = cartItem.Price * cartItem.Quantity;                    // Get the first available color or default to 1
                    int defaultColorId = 1;  // Default color ID if nothing is found

                    // Try to get the first available color for any product
                    var firstColor = await _context.Colors.FirstOrDefaultAsync();
                    if (firstColor != null)
                    {
                        defaultColorId = firstColor.ColorId;
                    }                    // Create order detail
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        ColorId = defaultColorId,
                        Quantity = cartItem.Quantity > 0 ? cartItem.Quantity : 1, // Ensure valid quantity
                        UnitPrice = cartItem.Price > 0 ? cartItem.Price : 0,      // Ensure valid price
                        TotalPrice = totalPrice > 0 ? totalPrice : 0              // Ensure valid total
                    };

                    _context.OrderDetails.Add(orderDetail);

                    // Update product stock
                    product.Stock -= cartItem.Quantity;
                    _context.Products.Update(product);
                }                try
                {
                    // Save all changes including order details and product updates
                    await _context.SaveChangesAsync();

                    // Auto-create warranties for each order detail
                    await CreateWarrantiesForOrder(order.OrderId);

                    // If successful, commit the transaction
                    await transaction.CommitAsync();

                    // Clear cart
                    if (customerId.HasValue)
                    {
                        var dbCartItems = _context.CartItems.Where(ci => ci.CustomerId == customerId.Value);
                        _context.CartItems.RemoveRange(dbCartItems);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        HttpContext.Session.Remove(CART_SESSION_KEY);
                    }

                    return Json(new {
                        success = true,
                        message = "Đơn hàng đã được tạo thành công. Bảo hành đã được kích hoạt tự động.",
                        orderId = order.OrderId,
                        redirectToWarranty = true
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();

                    // Log detailed error info
                    System.Diagnostics.Debug.WriteLine($"Database update error: {dbEx.Message}");

                    string errorDetails = "Lỗi cơ sở dữ liệu khi lưu đơn hàng: " + dbEx.Message;

                    if (dbEx.InnerException != null)
                    {
                        string innerMessage = dbEx.InnerException.Message;
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {innerMessage}");
                        errorDetails += "\nChi tiết: " + innerMessage;

                        // Check for specific error types
                        if (innerMessage.Contains("FK_OrderDetails_Colors"))
                        {
                            errorDetails = "Lỗi khi chọn màu sắc sản phẩm. Vui lòng thử lại.";
                        }
                        else if (innerMessage.Contains("FK_OrderDetails_Products"))
                        {
                            errorDetails = "Lỗi với thông tin sản phẩm. Vui lòng thử lại.";
                        }
                        else if (innerMessage.Contains("FK_Orders_ShippingAddresses"))
                        {
                            errorDetails = "Lỗi với địa chỉ giao hàng. Vui lòng kiểm tra lại thông tin địa chỉ.";
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Full exception: {dbEx}");
                    return Json(new { success = false, message = errorDetails });
                }
            }            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string errorMessage = "Có lỗi xảy ra khi tạo đơn hàng: " + ex.Message;

                // Get inner exception details
                if (ex.InnerException != null)
                {
                    errorMessage += " Chi tiết lỗi: " + ex.InnerException.Message;

                    // Check for deeper nested exceptions
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += " Chi tiết bổ sung: " + ex.InnerException.InnerException.Message;
                    }
                }

                // Check if this is a specific type of exception that we can handle
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                {
                    errorMessage = "Lỗi cập nhật cơ sở dữ liệu. Vui lòng thử lại.";
                }

                // Log the complete exception stack trace for debugging
                System.Diagnostics.Debug.WriteLine("Order creation error: " + errorMessage);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);

                return Json(new { success = false, message = errorMessage });
            }
        }        // GET: /checkout/confirmation/{orderId}
        public async Task<IActionResult> Confirmation(int orderId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId.Value);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Orders", "Customer");
            }

            var viewModel = new OrderConfirmationViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate ?? DateTime.Now,
                Status = order.Status ?? "Đang xử lý",
                PaymentMethod = order.PaymentMethod ?? "",
                Notes = order.Notes,
                TotalAmount = order.TotalAmount ?? 0,                Items = order.OrderDetails.Select(od => new CartItemViewModel
                {
                    ProductId = od.ProductId ?? 0,
                    ProductName = od.Product?.Name ?? "",
                    Price = od.UnitPrice,
                    Quantity = od.Quantity ?? 0,
                    ImageUrl = od.Product?.ProductImages?.FirstOrDefault()?.ImageUrl
                }).ToList(),ShippingAddress = order.ShippingAddress != null ? new ShippingAddressViewModel
                {
                    AddressId = order.ShippingAddress.AddressId,
                    RecipientName = order.ShippingAddress.RecipientName ?? "",
                    Phone = order.ShippingAddress.Phone ?? "",
                    AddressLine = order.ShippingAddress.AddressLine ?? "",
                    Ward = order.ShippingAddress.Ward ?? "",
                    District = order.ShippingAddress.District ?? "",
                    Province = order.ShippingAddress.Province ?? ""
                } : null,
                Customer = order.Customer != null ? new CustomerViewModel
                {
                    Id = order.Customer.CustomerId,
                    Name = order.Customer.Name ?? "",
                    Email = order.Customer.Email ?? "",
                    Phone = order.Customer.Phone ?? ""
                } : null
            };

            return View(viewModel);
        }

        // Helper method to get cart from session or database
        private Cart GetCart()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            
            if (customerId.HasValue)
            {
                var cart = new Cart();
                var items = _context.CartItems
                    .Include(ci => ci.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Include(ci => ci.Color)
                    .Where(ci => ci.CustomerId == customerId.Value)
                    .ToList();

                foreach (var item in items)
                {
                    cart.AddItem(item.ProductId, item.Product.ProductName, item.Product.Price, item.Quantity,
                        item.Product.ProductImages.FirstOrDefault()?.ImageUrl);
                }
                return cart;
            }

            var cartJson = HttpContext.Session.GetString(CART_SESSION_KEY);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new Cart();
            }

            return JsonSerializer.Deserialize<Cart>(cartJson) ?? new Cart();
        }

        // Helper method to automatically create warranties for order details
        private async Task CreateWarrantiesForOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            var customerIdForWarranty = order?.CustomerId ?? 0;

            var orderDetails = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderId == orderId)
                .ToListAsync();

            foreach (var orderDetail in orderDetails)
            {
                if (orderDetail.Product != null)
                {
                    // Generate unique warranty code
                    string warrantyCode;
                    do
                    {
                        warrantyCode = $"WR{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
                    }
                    while (await _context.Warranties.AnyAsync(w => w.WarrantyCode == warrantyCode));

                    // Calculate warranty end date based on product warranty period
                    var warrantyPeriodMonths = orderDetail.Product.WarrantyPeriodMonths > 0 
                        ? orderDetail.Product.WarrantyPeriodMonths 
                        : 12; // Default 12 months if not specified

                    var warranty = new Warranty
                    {
                        OrderDetailId = orderDetail.OrderDetailId,
                        CustomerId = customerIdForWarranty,
                        WarrantyCode = warrantyCode,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(warrantyPeriodMonths),
                        WarrantyPeriodMonths = warrantyPeriodMonths,
                        Status = Warranty.WarrantyStatus.Active,
                        Notes = $"Bảo hành tự động cho sản phẩm {orderDetail.Product.Name}",
                        CreatedDate = DateTime.Now
                    };

                    _context.Warranties.Add(warranty);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
