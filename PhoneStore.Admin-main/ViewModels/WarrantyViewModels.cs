using PhoneStore.Models;

namespace PhoneStore.ViewModels
{
    public class WarrantyIndexViewModel
    {
        public List<WarrantyItemViewModel> Warranties { get; set; } = new List<WarrantyItemViewModel>();
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalWarranties { get; set; }
        public int ActiveWarranties { get; set; }
        public int ExpiringWarranties { get; set; }
        public int ClaimedWarranties { get; set; }
        public int WarrantiesNeedingService { get; set; }
        public List<WarrantyItemViewModel> WarrantiesWithPendingClaims { get; set; } = new List<WarrantyItemViewModel>();
    }

    public class WarrantyItemViewModel
    {
        public int WarrantyId { get; set; }
        public string WarrantyCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
        public int ClaimsCount { get; set; }
    }

    public class WarrantyListViewModel
    {
        public List<Warranty> Warranties { get; set; } = new List<Warranty>();
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalWarranties { get; set; }
    }

    public class WarrantyClaimListViewModel
    {
        public List<WarrantyClaim> Claims { get; set; } = new List<WarrantyClaim>();
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalClaims { get; set; }
    }

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
        public string IssueDescription { get; set; } = null!;
        public string IssueType { get; set; } = WarrantyClaim.ClaimIssueType.Hardware;
        public Warranty? Warranty { get; set; }
    }
}
