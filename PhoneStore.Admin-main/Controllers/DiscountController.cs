using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.ViewModels;
using PhoneStore.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStore.Controllers
{
    public class DiscountController : Controller
    {
        private readonly PhoneStoreContext _context;
        private const int PageSize = 10;

        public DiscountController(PhoneStoreContext context)
        {
            _context = context;        }

        [AdminAuthorize(area: "Discount", action: "Index")]
        public async Task<IActionResult> Index(string? searchString, string? sortOrder, int? pageNumber)
        {
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PercentSortParam"] = sortOrder == "percent" ? "percent_desc" : "percent";
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            var discounts = from d in _context.DiscountPrograms.Include(d => d.Products)
                           select d;

            if (!string.IsNullOrEmpty(searchString))
            {
                discounts = discounts.Where(d => d.DiscountName != null && d.DiscountName.Contains(searchString));
            }

            discounts = sortOrder switch
            {
                "name_desc" => discounts.OrderByDescending(d => d.DiscountName),
                "percent" => discounts.OrderBy(d => d.DiscountPercent),
                "percent_desc" => discounts.OrderByDescending(d => d.DiscountPercent),
                _ => discounts.OrderBy(d => d.DiscountName),
            };

            return View(await PaginatedList<DiscountProgram>.CreateAsync(discounts, pageNumber ?? 1, PageSize));        }

        [AdminAuthorize(area: "Discount", action: "Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DiscountProgram discount)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(discount);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        [AdminAuthorize(area: "Discount", action: "Edit")]
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] DiscountProgram discount)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingDiscount = await _context.DiscountPrograms.FindAsync(discount.DiscountId);
                    if (existingDiscount == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy chương trình giảm giá" });
                    }

                    existingDiscount.DiscountName = discount.DiscountName;
                    existingDiscount.DiscountPercent = discount.DiscountPercent;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        [AdminAuthorize(area: "Discount", action: "Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _context.DiscountPrograms
                .Include(d => d.Products)
                .FirstOrDefaultAsync(d => d.DiscountId == id);

            if (discount == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chương trình giảm giá" });
            }

            if (discount.Products.Any())
            {
                return Json(new { success = false, message = "Không thể xóa chương trình giảm giá đang được sử dụng trong sản phẩm" });
            }

            try
            {
                _context.DiscountPrograms.Remove(discount);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
