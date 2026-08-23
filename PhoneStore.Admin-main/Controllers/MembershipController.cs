using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.Attributes;

namespace PhoneStore.Controllers
{
    public class MembershipController : Controller
    {
        private readonly PhoneStoreContext _context;

        public MembershipController(PhoneStoreContext context)
        {
            _context = context;
        }

        [AdminAuthorize(area: "Membership", action: "Index")]
        public async Task<IActionResult> Index()
        {
            var memberships = await _context.Memberships
                .Include(m => m.Customers)
                .OrderBy(m => m.Name)
                .ToListAsync();
            return View(memberships);
        }

        [AdminAuthorize(area: "Membership", action: "Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Membership membership)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra tên gói thành viên đã tồn tại chưa
                    var existingMembership = await _context.Memberships
                        .FirstOrDefaultAsync(m => m.Name.ToLower() == membership.Name.ToLower());

                    if (existingMembership != null)
                    {
                        return Json(new { success = false, message = "Tên gói thành viên đã tồn tại" });
                    }

                    membership.CreatedDate = DateTime.Now;
                    _context.Memberships.Add(membership);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Thêm gói thành viên thành công" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        [AdminAuthorize(area: "Membership", action: "Edit")]
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] Membership membership)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingMembership = await _context.Memberships.FindAsync(membership.MembershipId);
                    if (existingMembership == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy gói thành viên" });
                    }

                    // Kiểm tra tên gói thành viên đã tồn tại chưa (trừ gói hiện tại)
                    var duplicateMembership = await _context.Memberships
                        .FirstOrDefaultAsync(m => m.Name.ToLower() == membership.Name.ToLower()
                                                  && m.MembershipId != membership.MembershipId);

                    if (duplicateMembership != null)
                    {
                        return Json(new { success = false, message = "Tên gói thành viên đã tồn tại" });
                    }

                    existingMembership.Name = membership.Name;
                    existingMembership.Description = membership.Description;
                    existingMembership.DiscountPercentage = membership.DiscountPercentage;
                    existingMembership.MinimumSpend = membership.MinimumSpend;
                    existingMembership.IsActive = membership.IsActive;
                    existingMembership.UpdatedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Cập nhật gói thành viên thành công" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        [AdminAuthorize(area: "Membership", action: "Delete")]
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var membership = await _context.Memberships
                    .Include(m => m.Customers)
                    .FirstOrDefaultAsync(m => m.MembershipId == id);

                if (membership == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy gói thành viên" });
                }

                if (membership.Customers.Any())
                {
                    return Json(new { success = false, message = $"Không thể xóa gói thành viên đang được sử dụng bởi {membership.Customers.Count} khách hàng" });
                }

                _context.Memberships.Remove(membership);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa gói thành viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [AdminAuthorize(area: "Membership", action: "ToggleStatus")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var membership = await _context.Memberships.FindAsync(id);
                if (membership == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy gói thành viên" });
                }

                membership.IsActive = !membership.IsActive;
                membership.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                var status = membership.IsActive ? "kích hoạt" : "vô hiệu hóa";
                return Json(new { success = true, message = $"Đã {status} gói thành viên thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [AdminAuthorize(area: "Membership", action: "GetDetails")]
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var membership = await _context.Memberships
                    .Include(m => m.Customers)
                    .FirstOrDefaultAsync(m => m.MembershipId == id);

                if (membership == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy gói thành viên" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        membershipId = membership.MembershipId,
                        name = membership.Name,
                        description = membership.Description,
                        discountPercentage = membership.DiscountPercentage,
                        minimumSpend = membership.MinimumSpend,
                        isActive = membership.IsActive,
                        customerCount = membership.Customers.Count,
                        createdDate = membership.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                        updatedDate = membership.UpdatedDate?.ToString("dd/MM/yyyy HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}
