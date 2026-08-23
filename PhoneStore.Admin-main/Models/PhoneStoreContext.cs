using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Models;

public partial class PhoneStoreContext : DbContext
{
    public PhoneStoreContext()
    {
    }

    public PhoneStoreContext(DbContextOptions<PhoneStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Color> Colors { get; set; }
    public virtual DbSet<Coupon> Coupons { get; set; }
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<DiscountProgram> DiscountPrograms { get; set; }
    public virtual DbSet<Membership> Memberships { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderDetail> OrderDetails { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<ShippingAddress> ShippingAddresses { get; set; }
    public virtual DbSet<Warranty> Warranties { get; set; }
    public virtual DbSet<WarrantyClaim> WarrantyClaims { get; set; }
    // XÓA override hard-code connection string, chỉ dùng khi options chưa cấu hình
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Có thể để trống hoặc throw exception nếu cần
            // optionsBuilder.UseSqlServer("<your-connection-string>");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Base entity configurations
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Area).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);

            // Many-to-many relationship between Role and Permission
            entity.HasMany(r => r.Permissions)
                  .WithMany(p => p.Roles)
                  .UsingEntity(j => j.ToTable("RolePermissions"));
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.NationalId).HasMaxLength(20);

            // One-to-many relationship between Role and Admin
            entity.HasOne(d => d.Role)
                  .WithMany(p => p.Admins)
                  .HasForeignKey(d => d.RoleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed data
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "SuperAdmin", IsSystem = true, Description = "Có toàn quyền trên hệ thống" },
            new Role { RoleId = 2, RoleName = "Admin", IsSystem = true, Description = "Quản lý hệ thống" },
            new Role { RoleId = 3, RoleName = "User", IsSystem = true, Description = "Người dùng thông thường" }
        );        modelBuilder.Entity<Permission>().HasData(
            new Permission { PermissionId = 1, Name = "ViewProducts", Description = "Xem danh sách sản phẩm", Area = "Product", Action = "Index" },
            new Permission { PermissionId = 2, Name = "CreateProduct", Description = "Thêm sản phẩm mới", Area = "Product", Action = "Create" },
            new Permission { PermissionId = 3, Name = "EditProduct", Description = "Sửa sản phẩm", Area = "Product", Action = "Edit" },
            new Permission { PermissionId = 4, Name = "DeleteProduct", Description = "Xóa sản phẩm", Area = "Product", Action = "Delete" },
            new Permission { PermissionId = 5, Name = "ManageRoles", Description = "Quản lý phân quyền", Area = "Role", Action = "*" },
            new Permission { PermissionId = 6, Name = "ViewMemberships", Description = "Xem danh sách gói thành viên", Area = "Membership", Action = "Index" },
            new Permission { PermissionId = 7, Name = "CreateMembership", Description = "Thêm gói thành viên mới", Area = "Membership", Action = "Create" },
            new Permission { PermissionId = 8, Name = "EditMembership", Description = "Sửa gói thành viên", Area = "Membership", Action = "Edit" },
            new Permission { PermissionId = 9, Name = "DeleteMembership", Description = "Xóa gói thành viên", Area = "Membership", Action = "Delete" },
            new Permission { PermissionId = 10, Name = "ToggleMembershipStatus", Description = "Bật/tắt trạng thái gói thành viên", Area = "Membership", Action = "ToggleStatus" },
            new Permission { PermissionId = 11, Name = "GetMembershipDetails", Description = "Xem chi tiết gói thành viên", Area = "Membership", Action = "GetDetails" }
        );        // Seed default admin account
        modelBuilder.Entity<Admin>().HasData(
            new Admin
            {
                AdminId = 1,
                Username = "admin",
                PasswordHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3", // "123"
                IsApproved = true,
                IsBlocked = false,
                RoleId = 1 // SuperAdmin
            }
        );

        // Seed membership data
        modelBuilder.Entity<Membership>().HasData(
            new Membership
            {
                MembershipId = 1,
                Name = "Khách hàng thường",
                Description = "Gói thành viên cơ bản cho khách hàng mới",
                DiscountPercentage = 0,
                MinimumSpend = 0,
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 1)
            },
            new Membership
            {
                MembershipId = 2,
                Name = "Khách hàng VIP",
                Description = "Gói thành viên VIP với ưu đãi đặc biệt",
                DiscountPercentage = 10,
                MinimumSpend = 1000000,
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 1)
            },
            new Membership
            {
                MembershipId = 3,
                Name = "Khách hàng Premium",
                Description = "Gói thành viên cao cấp với nhiều quyền lợi",
                DiscountPercentage = 15,
                MinimumSpend = 5000000,
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 1)
            }
        );

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B1A060B7F");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Color>(entity =>
        {
            entity.HasKey(e => e.ColorId).HasName("PK__Colors__8DA7676D960B1EFD");

            entity.Property(e => e.ColorId).HasColumnName("ColorID");
            entity.Property(e => e.ColorName).HasMaxLength(50);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Coupons__384AF1DA52713C75");

            entity.HasIndex(e => e.Code, "UQ__Coupons__A25C5AA7E5220838").IsUnique();

            entity.Property(e => e.CouponId).HasColumnName("CouponID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.UsedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B8743F7D9A");

            entity.HasIndex(e => e.Email, "UQ__Customer__A9D10534F1B2ABE2").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MembershipId).HasColumnName("MembershipID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.Membership).WithMany(p => p.Customers)
                .HasForeignKey(d => d.MembershipId)
                .HasConstraintName("FK__Customers__Membe__286302EC");
        });

        modelBuilder.Entity<DiscountProgram>(entity =>
        {
            entity.HasKey(e => e.DiscountId).HasName("PK__Discount__E43F6DF6B960DF06");

            entity.Property(e => e.DiscountId).HasColumnName("DiscountID");
            entity.Property(e => e.DiscountName).HasMaxLength(100);
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(e => e.MembershipId).HasName("PK__Membersh__92A785998E06C222");

            entity.Property(e => e.MembershipId).HasColumnName("MembershipID");
            entity.Property(e => e.Name).HasMaxLength(100);
            
            // Cấu hình precision và scale cho các trường decimal
            entity.Property(e => e.DiscountPercentage)
                .HasPrecision(5, 2); // Cho phép tối đa 999.99%
            
            entity.Property(e => e.MinimumSpend)
                .HasPrecision(18, 2); // Cho phép số tiền lớn với 2 chữ số thập phân
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAFF8F24164");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CouponId).HasColumnName("CouponID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ShippingAddressId).HasColumnName("ShippingAddressID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK__Orders__CouponID__4CA06362");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__Customer__4AB81AF0");

            entity.HasOne(d => d.ShippingAddress).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingAddressId)
                .HasConstraintName("FK__Orders__Shipping__4BAC3F29");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30CEB60F853");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.ColorId).HasColumnName("ColorID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Color).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ColorId)
                .HasConstraintName("FK__OrderDeta__Color__5165187F");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__4F7CD00D");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderDeta__Produ__5070F446");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6EDABF82D83");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.DiscountId).HasColumnName("DiscountID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.ShortDescription).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__37A5467C");

            entity.HasOne(d => d.Discount).WithMany(p => p.Products)
                .HasForeignKey(d => d.DiscountId)
                .HasConstraintName("FK__Products__Discou__38996AB5");
        });        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F4ECDB2A4B03");

            entity.Property(e => e.ImageId).HasColumnName("ImageID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ColorId)
                .HasColumnType("int")
                .HasColumnName("ColorID");

            entity.Property(e => e.ImageData)
                .IsRequired()
                .HasColumnType("varbinary(max)");

            entity.Property(e => e.ImageMimeType)
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            entity.Property(e => e.ProductId)
                .HasColumnType("int")
                .HasColumnName("ProductID");

            entity.HasIndex(e => e.ColorId);
            entity.HasIndex(e => e.ProductId);

            entity.HasOne(d => d.Color).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ColorId)
                .HasConstraintName("FK__ProductIm__Color__3E52440B");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductIm__Produ__3D5E1FD2");
        });
        modelBuilder.Entity<ShippingAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__ShippingAddresses__AddressID");

            entity.Property(e => e.AddressId)
                .HasColumnName("AddressID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CustomerId)
                .HasColumnName("CustomerID");

            entity.Property(e => e.RecipientName)
                .HasMaxLength(100);

            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.Property(e => e.AddressLine)
                .HasMaxLength(255);

            entity.Property(e => e.Ward)
                .HasMaxLength(100);

            entity.Property(e => e.District)
                .HasMaxLength(100);

            entity.Property(e => e.Province)
                .HasMaxLength(100);

            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false);

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.ShippingAddresses)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__ShippingAddresses__CustomerID");
        });

        // Cấu hình cho Warranty
        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.HasKey(e => e.WarrantyId);

            entity.Property(e => e.WarrantyCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.WarrantyCode)
                .IsUnique();

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue(Warranty.WarrantyStatus.Active);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            // Relationships
            entity.HasOne(d => d.OrderDetail)
                .WithMany()
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Cấu hình cho WarrantyClaim
        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.HasKey(e => e.WarrantyClaimId);

            entity.Property(e => e.ClaimCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.ClaimCode)
                .IsUnique();

            entity.Property(e => e.IssueDescription)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.IssueType)
                .HasMaxLength(20)
                .HasDefaultValue(WarrantyClaim.ClaimIssueType.Hardware);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue(WarrantyClaim.ClaimStatus.Pending);

            entity.Property(e => e.AdminNotes)
                .HasMaxLength(1000);

            entity.Property(e => e.Resolution)
                .HasMaxLength(1000);

            entity.Property(e => e.ResolutionType)
                .HasMaxLength(20);

            entity.Property(e => e.ProcessedByAdmin)
                .HasMaxLength(100);

            entity.Property(e => e.SubmittedDate)
                .HasDefaultValueSql("GETDATE()");

            // Relationships
            entity.HasOne(d => d.Warranty)
                .WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.WarrantyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
