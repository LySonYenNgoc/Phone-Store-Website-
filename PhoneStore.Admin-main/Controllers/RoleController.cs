using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.Attributes;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace PhoneStore.Controllers
{
    // Sử dụng phân quyền chi tiết trên Controller
    [AdminAuthorize(area: "Role", action: "View")]
    public class RoleController : Controller
    {
        private readonly PhoneStoreContext _context;
        private readonly ILogger<RoleController> _logger;

        public RoleController(PhoneStoreContext context, ILogger<RoleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .Include(r => r.Permissions)
                .Include(r => r.Admins)
                .ToListAsync();

            return View(roles);
        }

        [AdminAuthorize(area: "Role", action: "Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                return NotFound();
            }

            // Lấy danh sách tất cả permissions
            var allPermissions = await _context.Set<Permission>().ToListAsync();
            ViewBag.AllPermissions = allPermissions;

            return View(role);
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(area: "Role", action: "Edit")]
        public async Task<IActionResult> Edit(int id, List<int> selectedPermissions)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                return NotFound();
            }

            if (role.IsSystem && role.RoleName == "SuperAdmin")
            {
                // Không cho phép chỉnh sửa quyền của SuperAdmin
                TempData["Error"] = "Không thể chỉnh sửa quyền của SuperAdmin";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Cập nhật permissions cho role
                var permissions = await _context.Set<Permission>()
                    .Where(p => selectedPermissions.Contains(p.PermissionId))
                    .ToListAsync();

                role.Permissions.Clear();
                foreach (var permission in permissions)
                {
                    role.Permissions.Add(permission);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật quyền thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật quyền cho role {RoleId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật quyền";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(area: "Role", action: "Create")]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tạo role mới thành công";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo role mới");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo role mới");
                }
            }

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize(area: "Role", action: "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            if (role.IsSystem)
            {
                return Json(new { success = false, message = "Không thể xóa role hệ thống" });
            }

            try
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa role {RoleId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa role" });
            }
        }
    }
}
