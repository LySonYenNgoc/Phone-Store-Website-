using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Customer.Models
{
    public partial class WarrantyClaim
    {
        public int WarrantyClaimId { get; set; }

        [Required]
        public int WarrantyId { get; set; }

        [Required]
        [StringLength(50)]
        public string ClaimCode { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string IssueDescription { get; set; } = null!;

        [StringLength(20)]
        public string IssueType { get; set; } = ClaimIssueType.Hardware;

        [StringLength(20)]
        public string Status { get; set; } = ClaimStatus.Pending;

        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        [StringLength(1000)]
        public string? Resolution { get; set; }

        [StringLength(20)]
        public string? ResolutionType { get; set; }

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public DateTime? ProcessedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(100)]
        public string? ProcessedByAdmin { get; set; }

        // Navigation properties
        public virtual Warranty Warranty { get; set; } = null!;

        // Trạng thái yêu cầu bảo hành
        public static class ClaimStatus
        {
            public const string Pending = "Chờ xử lý";
            public const string InProgress = "Đang xử lý";
            public const string Approved = "Đã duyệt";
            public const string Rejected = "Từ chối";
            public const string Completed = "Hoàn thành";
            public const string Cancelled = "Đã hủy";

            public static List<string> AllStatuses = new List<string>
            {
                Pending,
                InProgress,
                Approved,
                Rejected,
                Completed,
                Cancelled
            };
        }

        // Loại vấn đề
        public static class ClaimIssueType
        {
            public const string Hardware = "Lỗi phần cứng";
            public const string Software = "Lỗi phần mềm";
            public const string Screen = "Lỗi màn hình";
            public const string Battery = "Lỗi pin";
            public const string Camera = "Lỗi camera";
            public const string Audio = "Lỗi âm thanh";
            public const string Connectivity = "Lỗi kết nối";
            public const string Other = "Lỗi khác";

            public static List<string> AllTypes = new List<string>
            {
                Hardware,
                Software,
                Screen,
                Battery,
                Camera,
                Audio,
                Connectivity,
                Other
            };
        }

        // Loại giải quyết
        public static class ResolutionTypes
        {
            public const string Repair = "Sửa chữa";
            public const string Replace = "Thay thế";
            public const string Refund = "Hoàn tiền";
            public const string NoAction = "Không hành động";

            public static List<string> AllTypes = new List<string>
            {
                Repair,
                Replace,
                Refund,
                NoAction
            };
        }
    }
}
