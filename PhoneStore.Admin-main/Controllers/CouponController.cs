using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.Attributes;

namespace PhoneStore.Controllers
{
    [AdminAuthorize]
    public class CouponController : Controller
    {
        private readonly PhoneStoreContext _context;

        public CouponController(PhoneStoreContext context)
        {
            _context = context;
        }

        // GET: Coupon
        public async Task<IActionResult> Index(string searchTerm, string statusFilter, int page = 1, int pageSize = 10)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;

            var couponsQuery = _context.Coupons.AsQueryable();

            // Search by code
            if (!string.IsNullOrEmpty(searchTerm))
            {
                couponsQuery = couponsQuery.Where(c => c.Code!.Contains(searchTerm));
            }

            // Filter by status
            switch (statusFilter)
            {
                case "active":
                    couponsQuery = couponsQuery.Where(c =>
                        c.ExpiryDate > DateTime.Now && (c.IsUsed == false || c.IsUsed == null));
                    break;
                case "expired":
                    couponsQuery = couponsQuery.Where(c => c.ExpiryDate <= DateTime.Now);
                    break;
                case "used":
                    couponsQuery = couponsQuery.Where(c => c.IsUsed == true);
                    break;
            }

            var totalCount = await couponsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var coupons = await couponsQuery
                .OrderBy(c => c.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(coupons);
        }

        // GET: Coupon/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(m => m.CouponId == id);

            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // GET: Coupon/Create
        public IActionResult Create()
        {
            var coupon = new Coupon
            {
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddMonths(1),
                IsUsed = false
            };
            return View(coupon);
        }

        // POST: Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,DiscountAmount,ExpiryDate")] Coupon coupon)
        {
            // Validate coupon code uniqueness
            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                ModelState.AddModelError("Code", "Mã coupon đã tồn tại. Vui lòng chọn mã khác.");
            }

            // Validate expiry date
            if (coupon.ExpiryDate <= DateTime.Now)
            {
                ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải sau ngày hiện tại.");
            }

            // Validate discount amount
            if (coupon.DiscountAmount <= 0)
            {
                ModelState.AddModelError("DiscountAmount", "Số tiền giảm giá phải lớn hơn 0.");
            }

            if (ModelState.IsValid)
            {
                coupon.CreatedDate = DateTime.Now;
                coupon.IsUsed = false;
                _context.Add(coupon);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo coupon thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // GET: Coupon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        // POST: Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CouponId,Code,DiscountAmount,CreatedDate,ExpiryDate,UsedDate,IsUsed")] Coupon coupon)
        {
            if (id != coupon.CouponId)
            {
                return NotFound();
            }

            // Validate coupon code uniqueness (excluding current coupon)
            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code && c.CouponId != coupon.CouponId))
            {
                ModelState.AddModelError("Code", "Mã coupon đã tồn tại. Vui lòng chọn mã khác.");
            }

            // Validate expiry date for unused coupons
            if (coupon.IsUsed != true && coupon.ExpiryDate <= DateTime.Now)
            {
                ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải sau ngày hiện tại cho coupon chưa sử dụng.");
            }

            // Validate discount amount
            if (coupon.DiscountAmount <= 0)
            {
                ModelState.AddModelError("DiscountAmount", "Số tiền giảm giá phải lớn hơn 0.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật coupon thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.CouponId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // GET: Coupon/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(m => m.CouponId == id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // POST: Coupon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                // Check if coupon is used in any orders
                var hasOrders = await _context.Orders.AnyAsync(o => o.CouponId == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa coupon đã được sử dụng trong đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa coupon thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Coupon/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            // Only toggle used status for non-expired coupons
            if (coupon.ExpiryDate > DateTime.Now)
            {
                coupon.IsUsed = !coupon.IsUsed;
                if (coupon.IsUsed == true)
                {
                    coupon.UsedDate = DateTime.Now;
                }
                else
                {
                    coupon.UsedDate = null;
                }

                _context.Update(coupon);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.CouponId == id);
        }

        // Helper method to get coupon status
        public static string GetCouponStatus(Coupon coupon)
        {
            if (coupon.IsUsed == true)
                return "used";
            if (coupon.ExpiryDate <= DateTime.Now)
                return "expired";
            return "active";
        }

        // Helper method to get coupon status display text
        public static string GetCouponStatusText(Coupon coupon)
        {
            switch (GetCouponStatus(coupon))
            {
                case "used":
                    return "Đã sử dụng";
                case "expired":
                    return "Hết hạn";
                case "active":
                    return "Hoạt động";
                default:
                    return "Không xác định";
            }
        }

        // Helper method to get coupon status CSS class
        public static string GetCouponStatusClass(Coupon coupon)
        {
            switch (GetCouponStatus(coupon))
            {
                case "used":
                    return "bg-gray-100 text-gray-800";
                case "expired":
                    return "bg-red-100 text-red-800";
                case "active":
                    return "bg-green-100 text-green-800";
                default:
                    return "bg-gray-100 text-gray-800";
            }
        }
    }
}
