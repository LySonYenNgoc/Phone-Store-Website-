using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Customer.Models;
using PhoneStore.Customer.ViewModels;
using System.Text.Json;

namespace PhoneStore.Customer.Controllers
{
    public class CartController : Controller
    {
        private readonly PhoneStoreContext _context;
        private const string CART_SESSION_KEY = "Cart";

        public CartController(PhoneStoreContext context)
        {
            _context = context;
        }        public IActionResult Index()
        {
            var cart = GetCart();
            var viewModel = new CartViewModel
            {
                Items = cart.Items.Select(i => new CartItemViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    ImageUrl = i.ImageUrl
                }).ToList(),
                Total = cart.Total
            };
            return View(viewModel);
        }        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
        {
            try
            {                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }                if (product.Stock < request.Quantity)
                {
                    return Json(new { success = false, message = "Không đủ hàng trong kho" });
                }

                var cart = GetCart();
                cart.AddItem(product.ProductId, product.Name, product.Price, request.Quantity,
                    product.ProductImages.FirstOrDefault()?.ImageUrl);

                SaveCart(cart);

                return Json(new {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    cartCount = cart.Items.Sum(i => i.Quantity)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            try
            {
                var cart = GetCart();
                if (quantity <= 0)
                {
                    cart.RemoveItem(productId);
                }
                else
                {
                    cart.UpdateQuantity(productId, quantity);
                }

                SaveCart(cart);
                return Json(new { success = true, total = cart.Total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            try
            {
                var cart = GetCart();
                cart.RemoveItem(productId);
                SaveCart(cart);

                return Json(new {
                    success = true,
                    total = cart.Total,
                    cartCount = cart.Items.Sum(i => i.Quantity)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_SESSION_KEY);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetCount()
        {
            var cart = GetCart();
            return Json(new { count = cart.Items.Sum(i => i.Quantity) });
        }

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

        private void SaveCart(Cart cart)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId.HasValue)
            {
                // Sync to DB
                var existingItems = _context.CartItems.Where(ci => ci.CustomerId == customerId.Value).ToList();
                
                // Clear and rebuild or sync intelligently
                // Rebuild is simpler for now
                _context.CartItems.RemoveRange(existingItems);
                foreach (var item in cart.Items)
                {
                    _context.CartItems.Add(new CartItemEntity
                    {
                        CustomerId = customerId.Value,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        DateAdded = DateTime.Now
                    });
                }
                _context.SaveChanges();
                return;
            }

            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CART_SESSION_KEY, cartJson);
        }
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
