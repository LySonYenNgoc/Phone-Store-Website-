using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? CustomerId { get; set; }

    public int? ShippingAddressId { get; set; }

    public int? CouponId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }
    
    public string? Status { get; set; } = "Đang xử lý"; // Mặc định là "Đang xử lý"

    public string? PaymentMethod { get; set; }
    
    public string? Notes { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ShippingAddress? ShippingAddress { get; set; }
    
    // Các trạng thái đơn hàng
    public static class OrderStatus
    {
        public const string Processing = "Đang xử lý";
        public const string Confirmed = "Đã xác nhận";
        public const string Shipping = "Đang giao hàng";
        public const string Delivered = "Đã giao hàng";
        public const string Cancelled = "Đã hủy";
        
        public static List<string> AllStatuses = new List<string>
        {
            Processing,
            Confirmed,
            Shipping,
            Delivered,
            Cancelled
        };
    }
}
