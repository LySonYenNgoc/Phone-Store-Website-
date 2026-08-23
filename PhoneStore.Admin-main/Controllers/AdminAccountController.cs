using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PhoneStore.Models;
using PhoneStore.Attributes;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace PhoneStore.Controllers
{
    public class AdminAccountController : Controller
    {
        private readonly PhoneStoreContext _context;

        public AdminAccountController(PhoneStoreContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu";
                return View();
            }            var admin = await _context.Admins
                .Include(a => a.Role!).ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (admin == null || string.IsNullOrEmpty(admin.Username) || !VerifyPassword(password, admin.PasswordHash))
            {
                TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }

            if (!admin.IsApproved)
            {
                TempData["Error"] = "Tài khoản chưa được phê duyệt";
                return View();
            }

            if (admin.IsBlocked)
            {
                TempData["Error"] = "Tài khoản đã bị khoá";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim("AdminId", admin.AdminId.ToString()),
                new Claim(ClaimTypes.Role, admin.Role?.RoleName ?? "NoRole")
            };

            // Add permission claims
            if (admin.Role?.Permissions != null)
            {
                foreach (var permission in admin.Role.Permissions)
                {
                    claims.Add(new Claim("Permission", permission.Name));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }        private bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToHexString(hashedBytes).ToLower();
                return hash == storedHash;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            var password = "123";
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToHexString(hashedBytes).ToLower();
                ViewBag.PasswordHash = hash;
                ViewBag.ExpectedHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
                ViewBag.HashMatch = hash == "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
            }

            var admin = await _context.Admins
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == "admin");

            ViewBag.AdminFound = admin != null;
            if (admin != null)
            {
                ViewBag.AdminData = $"ID: {admin.AdminId}, Username: {admin.Username}, PasswordHash: {admin.PasswordHash}, IsApproved: {admin.IsApproved}, IsBlocked: {admin.IsBlocked}, Role: {admin.Role?.RoleName ?? "No Role"}";
            }

            var allAdmins = await _context.Admins.ToListAsync();
            ViewBag.AdminCount = allAdmins.Count;

            return View();        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Load available roles for dropdown - include Admin and User roles for registration
            var roles = await _context.Roles
                .Where(r => r.RoleName == "Admin" || r.RoleName == "User")
                .ToListAsync();            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string username, string password, string confirmPassword, string nationalId, int roleId)
        {
            // Load roles first to ensure dropdown is always populated
            var roles = await _context.Roles
                .Where(r => r.RoleName == "Admin" || r.RoleName == "User")
                .ToListAsync();
            ViewBag.Roles = roles;if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin bắt buộc";
                return View();
            }

            if (roleId <= 0)
            {
                ViewBag.Error = "Vui lòng chọn vai trò";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp";
                return View();
            }

            if (password.Length < 3)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 3 ký tự";
                return View();
            }

            // Check if username already exists
            var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
            if (existingAdmin != null)
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại";
                return View();
            }

            // Hash password
            string passwordHash;
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                passwordHash = Convert.ToHexString(hashedBytes).ToLower();
            }

            // Create new admin
            var newAdmin = new Admin
            {
                FullName = fullName,
                Username = username,
                PasswordHash = passwordHash,
                NationalId = nationalId,
                RoleId = roleId > 0 ? roleId : 2, // Default to Admin role if not specified
                IsApproved = false, // Requires approval
                IsBlocked = false
            };

            _context.Admins.Add(newAdmin);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Đăng ký thành công! Tài khoản của bạn đang chờ được phê duyệt.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DebugRoles()
        {
            var allRoles = await _context.Roles.ToListAsync();
            var filteredRoles = await _context.Roles
                .Where(r => !r.IsSystem || r.RoleName == "Admin")
                .ToListAsync();

            ViewBag.AllRoles = allRoles;
            ViewBag.FilteredRoles = filteredRoles;
            ViewBag.AllRolesCount = allRoles.Count;
            ViewBag.FilteredRolesCount = filteredRoles.Count;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckRoles()
        {
            var allRoles = await _context.Roles.ToListAsync();
            var availableRoles = await _context.Roles
                .Where(r => !r.IsSystem || r.RoleName == "Admin")
                .ToListAsync();

            var result = new
            {
                TotalRoles = allRoles.Count,
                AvailableRoles = availableRoles.Count,
                AllRolesList = allRoles.Select(r => new { r.RoleId, r.RoleName, r.Description, r.IsSystem }).ToList(),
                AvailableRolesList = availableRoles.Select(r => new { r.RoleId, r.RoleName, r.Description, r.IsSystem }).ToList()
            };

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> TestRegister()
        {
            // Load available roles for dropdown - include Admin and User roles for registration
            var roles = await _context.Roles
                .Where(r => r.RoleName == "Admin" || r.RoleName == "User")
                .ToListAsync();
            ViewBag.Roles = roles;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestCreateAdmin()
        {
            try
            {
                // Test create an admin with roleId = 2 (Admin role)
                var roles = await _context.Roles.ToListAsync();
                var adminRole = roles.FirstOrDefault(r => r.RoleName == "Admin");

                if (adminRole == null)
                {
                    return Json(new { success = false, message = "Admin role not found", allRoles = roles.Select(r => new { r.RoleId, r.RoleName }).ToList() });
                }

                // Create test admin
                var testAdmin = new Admin
                {
                    FullName = "Test Admin",
                    Username = "testadmin" + DateTime.Now.Ticks,
                    PasswordHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3", // hash of "123"
                    RoleId = adminRole.RoleId,
                    IsApproved = false,
                    IsBlocked = false
                };

                _context.Admins.Add(testAdmin);
                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = "Test admin created successfully",
                    adminId = testAdmin.AdminId,
                    roleId = testAdmin.RoleId,
                    roleName = adminRole.RoleName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }        }        [AdminAuthorize("SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var admins = await _context.Admins
                .Include(a => a.Role)
                .ToListAsync();

            // Lấy tất cả role (trừ SuperAdmin nếu muốn)
            var roles = await _context.Roles.ToListAsync();
            ViewBag.Roles = roles;

            return View(admins);
        }

        [AdminAuthorize(area: "AdminAccount", action: "Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin != null)
            {
                admin.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [AdminAuthorize(area: "AdminAccount", action: "Approve")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeApproval(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin != null)
            {
                admin.IsApproved = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [AdminAuthorize(area: "AdminAccount", action: "Block")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin != null)
            {
                admin.IsBlocked = !admin.IsBlocked; // Toggle block status
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [AdminAuthorize(area: "AdminAccount", action: "Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin != null)
            {
                _context.Admins.Remove(admin);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }        [AdminAuthorize("SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleModel model)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(model.id);
                if (admin == null)
                {
                    return Json(new { success = false, message = "Admin not found" });
                }

                if (int.TryParse(model.role, out int roleId))
                {
                    admin.RoleId = roleId;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Role updated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid role value" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }        [AdminAuthorize(Roles = "SuperAdmin,Admin,User")]
        public async Task<IActionResult> Profile()
        {
            // Get current logged in admin's username from claims
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var admin = await _context.Admins
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (admin == null)
            {
                return RedirectToAction("Login");
            }

            return View(admin);
        }

        [AdminAuthorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, DateOnly? birthDate, string nationalId,
            string currentPassword, string newPassword, string confirmPassword)
        {
            // Get current logged in admin's username from claims
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var admin = await _context.Admins
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (admin == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                // Update basic profile information
                admin.FullName = fullName;
                admin.BirthDate = birthDate;
                admin.NationalId = nationalId;

                // Handle password change if provided
                if (!string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(newPassword))
                {
                    // Verify current password
                    if (!VerifyPassword(currentPassword, admin.PasswordHash))
                    {
                        TempData["Error"] = "Mật khẩu hiện tại không đúng";
                        return View("Profile", admin);
                    }

                    // Check if new password and confirm password match
                    if (newPassword != confirmPassword)
                    {
                        TempData["Error"] = "Mật khẩu mới và xác nhận mật khẩu không khớp";
                        return View("Profile", admin);
                    }

                    // Update password
                    admin.PasswordHash = HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin cá nhân thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật thông tin: " + ex.Message;
            }

            return View("Profile", admin);
        }        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Action cho trang Access Denied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
