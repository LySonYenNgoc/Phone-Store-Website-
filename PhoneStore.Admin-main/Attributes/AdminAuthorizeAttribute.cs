using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Models;

namespace PhoneStore.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[]? _requiredPermissions;
        private readonly string? _requiredArea;
        private readonly string? _requiredAction;
        
        public string? Roles { get; set; }
        public string? Permissions { get; set; }
        public string? Area { get; set; }
        public string? Action { get; set; }

        /// <summary>
        /// Yêu cầu xác thực admin và kiểm tra quyền
        /// </summary>
        /// <param name="permissions">Tên các quyền yêu cầu, cách nhau bởi dấu phẩy</param>
        /// <param name="area">Khu vực quản lý cần kiểm tra quyền</param>
        /// <param name="action">Hành động cần kiểm tra quyền (View, Create, Edit, Delete, ...)</param>
        public AdminAuthorizeAttribute(string? permissions = null, string? area = null, string? action = null)
        {
            Permissions = permissions;
            Area = area;
            Action = action;
            
            _requiredPermissions = permissions?.Split(',').Select(p => p.Trim()).ToArray();
            _requiredArea = area;
            _requiredAction = action;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var adminId = context.HttpContext.User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                context.Result = new RedirectToActionResult("Login", "AdminAccount", null);
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<PhoneStoreContext>();
            var admin = await dbContext.Admins
                .Include(a => a.Role!)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(a => a.AdminId == int.Parse(adminId));

            if (admin == null || !admin.IsApproved || admin.IsBlocked || admin.Role == null)
            {
                context.Result = new RedirectToActionResult("Login", "AdminAccount", null);
                return;
            }

            // SuperAdmin luôn có quyền truy cập mọi nơi
            if (admin.Role.RoleName == "SuperAdmin")
            {
                return;
            }

            // Kiểm tra Roles nếu được chỉ định
            if (!string.IsNullOrEmpty(Roles))
            {
                var allowedRoles = Roles.Split(',').Select(r => r.Trim());
                if (!allowedRoles.Contains(admin.Role.RoleName))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "AdminAccount", null);
                    return;
                }
            }

            // Kiểm tra Permissions nếu được chỉ định
            if (_requiredPermissions != null && _requiredPermissions.Length > 0)
            {
                var hasAllPermissions = _requiredPermissions.All(required =>
                    admin.Role.Permissions.Any(p => p.Name == required));

                if (!hasAllPermissions)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "AdminAccount", null);
                    return;
                }
            }
            
            // Kiểm tra Area và Action nếu được chỉ định
            if (!string.IsNullOrEmpty(_requiredArea) && !string.IsNullOrEmpty(_requiredAction))
            {
                var hasPermission = admin.Role.Permissions.Any(p => 
                    p.Area == _requiredArea && p.Action == _requiredAction);
                
                if (!hasPermission)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "AdminAccount", null);
                    return;
                }
            }
            // Chỉ kiểm tra Area
            else if (!string.IsNullOrEmpty(_requiredArea))
            {
                var hasPermission = admin.Role.Permissions.Any(p => p.Area == _requiredArea);
                
                if (!hasPermission)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "AdminAccount", null);
                    return;
                }
            }
            // Chỉ kiểm tra Action
            else if (!string.IsNullOrEmpty(_requiredAction))
            {
                var hasPermission = admin.Role.Permissions.Any(p => p.Action == _requiredAction);
                
                if (!hasPermission)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "AdminAccount", null);
                    return;
                }
            }
        }
    }
}
