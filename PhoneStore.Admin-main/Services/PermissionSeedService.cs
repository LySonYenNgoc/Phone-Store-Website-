using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneStore.Models;

namespace PhoneStore.Services
{
    public class PermissionSeedService
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionSeedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SeedPermissionsAndRoles()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PhoneStoreContext>();

                Console.WriteLine("Bắt đầu thiết lập quyền và vai trò...");

                // Kiểm tra nếu đã có dữ liệu trong bảng Permissions
                if (!await context.Permissions.AnyAsync())
                {
                    await SeedPermissions(context);
                }
                
                // Kiểm tra và tạo các vai trò nếu chưa tồn tại
                await EnsureRolesExist(context);
                
                // Gán quyền cho các vai trò
                await AssignPermissionsToRoles(context);
                
                Console.WriteLine("Hoàn thành thiết lập quyền và vai trò!");
            }
        }

        private async Task SeedPermissions(PhoneStoreContext context)
        {
            Console.WriteLine("Thêm danh sách quyền...");
            
            var permissions = new List<Permission>
            {
                // Quyền quản lý sản phẩm
                new Permission { PermissionId = 1, Name = "ViewProducts", Description = "Xem danh sách sản phẩm", Area = "Product", Action = "Index" },
                new Permission { PermissionId = 2, Name = "CreateProduct", Description = "Thêm sản phẩm mới", Area = "Product", Action = "Create" },
                new Permission { PermissionId = 3, Name = "EditProduct", Description = "Sửa sản phẩm", Area = "Product", Action = "Edit" },
                new Permission { PermissionId = 4, Name = "DeleteProduct", Description = "Xóa sản phẩm", Area = "Product", Action = "Delete" },
                        
                // Quyền quản lý danh mục
                new Permission { PermissionId = 5, Name = "ViewCategories", Description = "Xem danh sách danh mục", Area = "Category", Action = "Index" },
                new Permission { PermissionId = 6, Name = "CreateCategory", Description = "Thêm danh mục mới", Area = "Category", Action = "Create" },
                new Permission { PermissionId = 7, Name = "EditCategory", Description = "Sửa danh mục", Area = "Category", Action = "Edit" },
                new Permission { PermissionId = 8, Name = "DeleteCategory", Description = "Xóa danh mục", Area = "Category", Action = "Delete" },
                        
                // Quyền quản lý màu sắc
                new Permission { PermissionId = 9, Name = "ViewColors", Description = "Xem danh sách màu sắc", Area = "Color", Action = "Index" },
                new Permission { PermissionId = 10, Name = "CreateColor", Description = "Thêm màu sắc mới", Area = "Color", Action = "Create" },
                new Permission { PermissionId = 11, Name = "EditColor", Description = "Sửa màu sắc", Area = "Color", Action = "Edit" },
                new Permission { PermissionId = 12, Name = "DeleteColor", Description = "Xóa màu sắc", Area = "Color", Action = "Delete" },
                        
                // Quyền quản lý giảm giá
                new Permission { PermissionId = 13, Name = "ViewDiscounts", Description = "Xem chương trình giảm giá", Area = "Discount", Action = "Index" },
                new Permission { PermissionId = 14, Name = "CreateDiscount", Description = "Thêm chương trình giảm giá", Area = "Discount", Action = "Create" },
                new Permission { PermissionId = 15, Name = "EditDiscount", Description = "Sửa chương trình giảm giá", Area = "Discount", Action = "Edit" },
                new Permission { PermissionId = 16, Name = "DeleteDiscount", Description = "Xóa chương trình giảm giá", Area = "Discount", Action = "Delete" },
                        
                // Quyền quản lý đơn hàng
                new Permission { PermissionId = 17, Name = "ViewOrders", Description = "Xem danh sách đơn hàng", Area = "Order", Action = "Index" },
                new Permission { PermissionId = 18, Name = "CreateOrder", Description = "Tạo đơn hàng mới", Area = "Order", Action = "Create" },
                new Permission { PermissionId = 19, Name = "EditOrder", Description = "Cập nhật đơn hàng", Area = "Order", Action = "Edit" },
                new Permission { PermissionId = 20, Name = "DeleteOrder", Description = "Hủy đơn hàng", Area = "Order", Action = "Delete" },
                        
                // Quyền quản lý khách hàng
                new Permission { PermissionId = 21, Name = "ViewCustomers", Description = "Xem danh sách khách hàng", Area = "Customer", Action = "Index" },
                new Permission { PermissionId = 22, Name = "CreateCustomer", Description = "Thêm khách hàng mới", Area = "Customer", Action = "Create" },
                new Permission { PermissionId = 23, Name = "EditCustomer", Description = "Sửa thông tin khách hàng", Area = "Customer", Action = "Edit" },
                new Permission { PermissionId = 24, Name = "DeleteCustomer", Description = "Xóa khách hàng", Area = "Customer", Action = "Delete" },
                        
                // Quyền quản lý tài khoản quản trị
                new Permission { PermissionId = 25, Name = "ViewAdmins", Description = "Xem danh sách tài khoản quản trị", Area = "AdminAccount", Action = "Index" },
                new Permission { PermissionId = 26, Name = "CreateAdmin", Description = "Thêm tài khoản quản trị mới", Area = "AdminAccount", Action = "Register" },
                new Permission { PermissionId = 27, Name = "EditAdmin", Description = "Sửa tài khoản quản trị", Area = "AdminAccount", Action = "Edit" },
                new Permission { PermissionId = 28, Name = "DeleteAdmin", Description = "Xóa tài khoản quản trị", Area = "AdminAccount", Action = "Delete" },
                        
                // Quyền quản lý phân quyền
                new Permission { PermissionId = 29, Name = "ViewRoles", Description = "Xem danh sách vai trò", Area = "Role", Action = "View" },
                new Permission { PermissionId = 30, Name = "CreateRole", Description = "Tạo vai trò mới", Area = "Role", Action = "Create" },
                new Permission { PermissionId = 31, Name = "EditRole", Description = "Sửa vai trò", Area = "Role", Action = "Edit" },
                new Permission { PermissionId = 32, Name = "DeleteRole", Description = "Xóa vai trò", Area = "Role", Action = "Delete" },
                        
                // Quyền quản lý dashboard
                new Permission { PermissionId = 33, Name = "ViewDashboard", Description = "Xem trang tổng quan", Area = "Dashboard", Action = "Index" },
                new Permission { PermissionId = 34, Name = "GenerateFakeOrders", Description = "Tạo đơn hàng mẫu", Area = "Dashboard", Action = "GenerateFakeOrders" }
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Đã thêm {permissions.Count} quyền vào cơ sở dữ liệu.");
        }

        private async Task EnsureRolesExist(PhoneStoreContext context)
        {
            Console.WriteLine("Kiểm tra và tạo các vai trò...");
            
            // Danh sách các vai trò cần có
            var requiredRoles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "SuperAdmin", Description = "Có toàn quyền trên hệ thống", IsSystem = true },
                new Role { RoleId = 2, RoleName = "Admin", Description = "Quản trị viên", IsSystem = true },
                new Role { RoleId = 3, RoleName = "User", Description = "Người dùng thông thường", IsSystem = true }
            };

            foreach (var role in requiredRoles)
            {
                // Kiểm tra vai trò đã tồn tại chưa
                var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == role.RoleName);
                
                if (existingRole == null)
                {
                    // Nếu chưa tồn tại thì thêm mới
                    await context.Roles.AddAsync(role);
                    Console.WriteLine($"Đã thêm vai trò: {role.RoleName}");
                }
                else
                {
                    // Nếu đã tồn tại thì cập nhật
                    existingRole.Description = role.Description;
                    existingRole.IsSystem = role.IsSystem;
                    context.Roles.Update(existingRole);
                    Console.WriteLine($"Đã cập nhật vai trò: {role.RoleName}");
                }
            }
            
            await context.SaveChangesAsync();
        }        private async Task AssignPermissionsToRoles(PhoneStoreContext context)
        {
            Console.WriteLine("Gán quyền cho các vai trò...");

            // Xóa các phân quyền cũ bằng cách xóa từ bảng trung gian
            // Lấy tất cả các vai trò và xóa các quyền hiện tại của chúng
            var roles = await context.Roles.Include(r => r.Permissions).ToListAsync();
            foreach (var role in roles)
            {
                role.Permissions.Clear();
            }
            await context.SaveChangesAsync();
            Console.WriteLine("Đã xóa các phân quyền cũ.");
            

            // Lấy danh sách quyền và vai trò
            var permissions = await context.Permissions.ToListAsync();
            var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SuperAdmin");
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");

            if (superAdminRole != null && permissions.Any())
            {
                // SuperAdmin có tất cả các quyền
                foreach (var permission in permissions)
                {
                    superAdminRole.Permissions.Add(permission);
                }
                Console.WriteLine("Đã gán tất cả quyền cho SuperAdmin.");
            }

            if (adminRole != null && permissions.Any())
            {
                // Admin có tất cả quyền trừ quyền quản lý Role và AdminAccount
                foreach (var permission in permissions.Where(p => 
                    p.Area != "Role" && 
                    (p.Area != "AdminAccount" || p.Action == "Profile")))
                {
                    adminRole.Permissions.Add(permission);
                }
                Console.WriteLine("Đã gán quyền cho Admin.");
            }

            if (userRole != null && permissions.Any())
            {
                // User chỉ có quyền xem
                foreach (var permission in permissions.Where(p => 
                    p.Action == "Index" || p.Action == "View"))
                {
                    userRole.Permissions.Add(permission);
                }
                Console.WriteLine("Đã gán quyền cho User.");
            }

            await context.SaveChangesAsync();
        }
    }
}
