using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.Attributes;

namespace PhoneStore.Controllers
{
    public class CategoryController : Controller
    {
        private readonly PhoneStoreContext _context;

        public CategoryController(PhoneStoreContext context)
        {
            _context = context;        }

        [AdminAuthorize(area: "Category", action: "Index")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();
            return View(categories);        }

        [AdminAuthorize(area: "Category", action: "Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return Json(new { success = false, message = "Tên danh mục không được để trống" });
                }

                // Kiểm tra tên danh mục đã tồn tại chưa
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName == categoryName);
                
                if (existingCategory != null)
                {
                    return Json(new { success = false, message = "Tên danh mục đã tồn tại" });
                }

                var category = new Category
                {
                    CategoryName = categoryName
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [AdminAuthorize(area: "Category", action: "Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int categoryId, string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return Json(new { success = false, message = "Tên danh mục không được để trống" });
                }

                var existingCategory = await _context.Categories.FindAsync(categoryId);
                if (existingCategory == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Kiểm tra tên danh mục đã tồn tại chưa (trừ danh mục hiện tại)
                var duplicateCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName == categoryName && c.CategoryId != categoryId);
                
                if (duplicateCategory != null)
                {
                    return Json(new { success = false, message = "Tên danh mục đã tồn tại" });
                }

                existingCategory.CategoryName = categoryName;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [AdminAuthorize(area: "Category", action: "Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                if (category.Products.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục đang được sử dụng trong sản phẩm" });
                }

                _context.Categories.Remove(category);
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
