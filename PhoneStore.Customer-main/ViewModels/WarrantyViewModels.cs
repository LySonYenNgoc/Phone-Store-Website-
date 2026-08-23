using PhoneStore.Customer.Models;

namespace PhoneStore.Customer.ViewModels
{
    public class CustomerWarrantyViewModel
    {
        public List<Warranty> Warranties { get; set; } = new List<Warranty>();
        public List<WarrantyClaim> Claims { get; set; } = new List<WarrantyClaim>();
        public int ActiveWarranties { get; set; }
        public int ExpiredWarranties { get; set; }
        public int PendingClaims { get; set; }
    }

    public class CreateWarrantyClaimViewModel
    {
        public int WarrantyId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng mô tả vấn đề")]
        [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string IssueDescription { get; set; } = null!;
        
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn loại vấn đề")]
        public string IssueType { get; set; } = WarrantyClaim.ClaimIssueType.Hardware;
        
        public Warranty? Warranty { get; set; }
    }

    public class WarrantyCheckViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập mã bảo hành")]
        [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessage = "Mã bảo hành không hợp lệ")]
        public string WarrantyCode { get; set; } = null!;
    }
}
