using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.ViewModels;
using PhoneStore.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStore.Controllers
{
    public class ColorController : Controller
    {
        private readonly PhoneStoreContext _context;
        private const int PageSize = 10;

        public ColorController(PhoneStoreContext context)
        {
            _context = context;
        }

        [AdminAuthorize(area: "Color", action: "Index")]
        public async Task<IActionResult> Index(string? searchString, string? sortOrder, int? pageNumber)
        {
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            var colors = from c in _context.Colors
                        select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                colors = colors.Where(c => c.ColorName != null && c.ColorName.Contains(searchString));
            }

            colors = sortOrder switch
            {
                "name_desc" => colors.OrderByDescending(c => c.ColorName),
                _ => colors.OrderBy(c => c.ColorName),
            };

            return View(await PaginatedList<Color>.CreateAsync(colors, pageNumber ?? 1, PageSize));
        }

        [AdminAuthorize(area: "Color", action: "Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string colorName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(colorName))
                {
                    return Json(new { success = false, message = "Tên màu không được để trống" });
                }

                // Kiểm tra tên màu đã tồn tại chưa
                var existingColor = await _context.Colors
                    .FirstOrDefaultAsync(c => c.ColorName == colorName);
                
                if (existingColor != null)
                {
                    return Json(new { success = false, message = "Tên màu đã tồn tại" });
                }

                var color = new Color
                {
                    ColorName = colorName
                };

                _context.Colors.Add(color);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [AdminAuthorize(area: "Color", action: "Edit")]
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] Color color)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(color.ColorName))
                {
                    return Json(new { success = false, message = "Tên màu không được để trống" });
                }

                var existingColor = await _context.Colors.FindAsync(color.ColorId);
                if (existingColor == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy màu" });
                }

                // Kiểm tra tên màu đã tồn tại chưa (trừ màu hiện tại)
                var duplicateColor = await _context.Colors
                    .FirstOrDefaultAsync(c => c.ColorName == color.ColorName && c.ColorId != color.ColorId);
                
                if (duplicateColor != null)
                {
                    return Json(new { success = false, message = "Tên màu đã tồn tại" });
                }

                existingColor.ColorName = color.ColorName;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [AdminAuthorize(area: "Color", action: "Delete")]
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var color = await _context.Colors
                .Include(c => c.ProductImages)
                .FirstOrDefaultAsync(c => c.ColorId == id);

            if (color == null)
            {
                return Json(new { success = false, message = "Không tìm thấy màu" });
            }

            if (color.ProductImages.Any())
            {
                return Json(new { success = false, message = "Không thể xóa màu đang được sử dụng trong sản phẩm" });
            }

            try
            {
                _context.Colors.Remove(color);
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
