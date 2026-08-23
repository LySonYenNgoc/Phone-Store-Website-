using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string? Code { get; set; }

    public decimal? DiscountAmount { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? UsedDate { get; set; }

    public bool? IsUsed { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
