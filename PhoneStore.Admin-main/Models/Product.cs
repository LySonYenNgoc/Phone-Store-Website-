using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models;

public partial class Product
{    public int ProductId { get; set; }    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá {1} ký tự")]
    public string ProductName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá {1} ký tự")]
    public string? ShortDescription { get; set; }

    public string? DetailDescription { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng tồn")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
    public int Stock { get; set; }    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    public int? DiscountId { get; set; }

    [Required]
    public bool IsPublished { get; set; } = true;

    // Thông tin bảo hành
    [Range(1, 60, ErrorMessage = "Thời gian bảo hành phải từ 1 đến 60 tháng")]
    public int WarrantyPeriodMonths { get; set; } = 12; // Thời gian bảo hành mặc định 12 tháng

    [StringLength(1000)]
    public string? WarrantyTerms { get; set; } // Điều khoản bảo hành

    public virtual Category? Category { get; set; }

    public virtual DiscountProgram? Discount { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
