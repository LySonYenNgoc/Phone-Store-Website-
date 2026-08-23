using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models;

public partial class Membership
{
    public int MembershipId { get; set; }

    [Required(ErrorMessage = "Tên gói thành viên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên gói thành viên không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
    public decimal? DiscountPercentage { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu phải lớn hơn hoặc bằng 0")]
    public decimal? MinimumSpend { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
