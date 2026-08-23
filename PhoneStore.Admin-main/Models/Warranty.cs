using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models
{
    public partial class Warranty
    {
        public int WarrantyId { get; set; }

        [Required]
        public int OrderDetailId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string WarrantyCode { get; set; } = null!; // Mã bảo hành duy nhất

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now; // Ngày bắt đầu bảo hành

        [Required]
        public DateTime EndDate { get; set; } // Ngày kết thúc bảo hành

        [Required]
        [Range(1, 60)] // Từ 1 đến 60 tháng
        public int WarrantyPeriodMonths { get; set; } = 12; // Thời gian bảo hành (tháng)

        [StringLength(20)]
        public string Status { get; set; } = WarrantyStatus.Active; // Trạng thái bảo hành

        [StringLength(500)]
        public string? Notes { get; set; } // Ghi chú

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        public virtual OrderDetail OrderDetail { get; set; } = null!;
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();

        // Trạng thái bảo hành
        public static class WarrantyStatus
        {
            public const string Active = "Đang bảo hành";
            public const string Expired = "Hết hạn";
            public const string Void = "Đã hủy";
            public const string Transferred = "Đã chuyển nhượng";

            public static List<string> AllStatuses = new List<string>
            {
                Active,
                Expired,
                Void,
                Transferred
            };
        }

        // Phương thức kiểm tra còn bảo hành không
        public bool IsActiveWarranty()
        {
            return Status == WarrantyStatus.Active && DateTime.Now <= EndDate;
        }

        // Phương thức tính số ngày còn bảo hành
        public int DaysRemaining()
        {
            if (!IsActiveWarranty()) return 0;
            return (EndDate - DateTime.Now).Days;
        }
    }
}
