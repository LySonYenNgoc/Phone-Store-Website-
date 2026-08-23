using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;
using PhoneStore.Attributes;
using PhoneStore.ViewModels;
using System.Text;

namespace PhoneStore.Controllers
{
    [AdminAuthorize]
    public class WarrantyController : Controller
    {
        private readonly PhoneStoreContext _context;
        private readonly ILogger<WarrantyController> _logger;

        public WarrantyController(PhoneStoreContext context, ILogger<WarrantyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Warranty
        public async Task<IActionResult> Index(string searchTerm, string status, int page = 1)
        {
            const int pageSize = 20;

            var warrantyQuery = _context.Warranties
                .Include(w => w.Customer)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(w => w.WarrantyClaims)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                warrantyQuery = warrantyQuery.Where(w =>
                    w.WarrantyCode.Contains(searchTerm) ||
                    w.Customer.Name.Contains(searchTerm) ||
                    w.Customer.Email.Contains(searchTerm) ||
                    w.OrderDetail.Product.ProductName.Contains(searchTerm));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                warrantyQuery = warrantyQuery.Where(w => w.Status == status);
            }

            var totalWarranties = await warrantyQuery.CountAsync();
            var warranties = await warrantyQuery
                .OrderByDescending(w => w.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WarrantyItemViewModel
                {
                    WarrantyId = w.WarrantyId,
                    WarrantyCode = w.WarrantyCode,
                    CustomerName = w.Customer.Name ?? "",
                    CustomerEmail = w.Customer.Email ?? "",
                    ProductName = w.OrderDetail.Product.ProductName ?? "",
                    StartDate = w.StartDate,
                    EndDate = w.EndDate,
                    Status = w.Status,
                    ClaimsCount = w.WarrantyClaims.Count()
                })
                .ToListAsync();

            // Tính toán thống kê
            var activeWarranties = await _context.Warranties
                .CountAsync(w => w.Status == Warranty.WarrantyStatus.Active);
            
            var expiringWarranties = await _context.Warranties
                .CountAsync(w => w.Status == Warranty.WarrantyStatus.Active && w.EndDate <= DateTime.Now.AddDays(30));
            
            var claimedWarranties = await _context.WarrantyClaims
                .CountAsync();

            // Lấy bảo hành có khiếu nại chưa xử lý (cần được bảo hành)
            var warrantiesNeedingService = await _context.Warranties
                .Include(w => w.Customer)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.WarrantyClaims)
                .Where(w => w.WarrantyClaims.Any(c => c.Status == WarrantyClaim.ClaimStatus.Pending || c.Status == WarrantyClaim.ClaimStatus.InProgress))
                .Select(w => new WarrantyItemViewModel
                {
                    WarrantyId = w.WarrantyId,
                    WarrantyCode = w.WarrantyCode,
                    CustomerName = w.Customer.Name,
                    CustomerEmail = w.Customer.Email,
                    ProductName = w.OrderDetail.Product.ProductName,
                    StartDate = w.StartDate,
                    EndDate = w.EndDate,
                    Status = w.Status,
                    ClaimsCount = w.WarrantyClaims.Count(c => c.Status == WarrantyClaim.ClaimStatus.Pending || c.Status == WarrantyClaim.ClaimStatus.InProgress)
                })
                .ToListAsync();

            var viewModel = new WarrantyIndexViewModel
            {
                Warranties = warranties,
                SearchTerm = searchTerm,
                Status = status,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalWarranties / pageSize),
                TotalWarranties = totalWarranties,
                ActiveWarranties = activeWarranties,
                ExpiringWarranties = expiringWarranties,
                ClaimedWarranties = claimedWarranties,
                WarrantiesNeedingService = warrantiesNeedingService.Count,
                WarrantiesWithPendingClaims = warrantiesNeedingService
            };

            ViewData["SearchTerm"] = searchTerm;
            ViewData["Status"] = status;

            return View(viewModel);
        }

        // GET: Warranty/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var warranty = await _context.Warranties
                .Include(w => w.Customer)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Color)
                .Include(w => w.WarrantyClaims)
                .FirstOrDefaultAsync(w => w.WarrantyId == id);

            if (warranty == null)
            {
                return NotFound();
            }

            return View(warranty);
        }

        // GET: Warranty/Create - Tạo bảo hành từ OrderDetail
        public async Task<IActionResult> Create(int? orderDetailId)
        {
            if (orderDetailId == null)
            {
                return BadRequest("OrderDetailId is required");
            }

            var orderDetail = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Customer)
                .Include(od => od.Color)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (orderDetail == null)
            {
                return NotFound("OrderDetail not found");
            }

            // Kiểm tra xem đã có bảo hành chưa
            var existingWarranty = await _context.Warranties
                .FirstOrDefaultAsync(w => w.OrderDetailId == orderDetailId);

            if (existingWarranty != null)
            {
                TempData["Error"] = "Sản phẩm này đã có bảo hành!";
                return RedirectToAction("Details", "Order", new { id = orderDetail.OrderId });
            }

            var warranty = new Warranty
            {
                OrderDetailId = orderDetailId.Value,
                CustomerId = orderDetail.Order.CustomerId ?? 0,
                WarrantyCode = GenerateWarrantyCode(),
                StartDate = DateTime.Now,
                WarrantyPeriodMonths = orderDetail.Product.WarrantyPeriodMonths,
                EndDate = DateTime.Now.AddMonths(orderDetail.Product.WarrantyPeriodMonths)
            };

            ViewBag.OrderDetail = orderDetail;
            return View(warranty);
        }

        // POST: Warranty/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Warranty warranty)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã bảo hành không trùng
                var existingCode = await _context.Warranties
                    .AnyAsync(w => w.WarrantyCode == warranty.WarrantyCode);

                if (existingCode)
                {
                    warranty.WarrantyCode = GenerateWarrantyCode();
                }

                warranty.CreatedDate = DateTime.Now;
                warranty.EndDate = warranty.StartDate.AddMonths(warranty.WarrantyPeriodMonths);

                _context.Warranties.Add(warranty);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tạo bảo hành thành công!";
                return RedirectToAction(nameof(Details), new { id = warranty.WarrantyId });
            }

            // Reload OrderDetail if validation fails
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Customer)
                .Include(od => od.Color)
                .FirstOrDefaultAsync(od => od.OrderDetailId == warranty.OrderDetailId);

            ViewBag.OrderDetail = orderDetail;
            return View(warranty);
        }

        // POST: Warranty/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int warrantyId, string status, string notes)
        {
            var warranty = await _context.Warranties.FindAsync(warrantyId);
            if (warranty == null)
            {
                return NotFound();
            }

            warranty.Status = status;
            warranty.Notes = notes;
            warranty.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái bảo hành thành công!";
            return RedirectToAction(nameof(Details), new { id = warrantyId });
        }

        // GET: Warranty/Claims
        public async Task<IActionResult> Claims(string searchTerm, string status, int page = 1)
        {
            const int pageSize = 20;

            var claimsQuery = _context.WarrantyClaims
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.Customer)
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.OrderDetail)
                        .ThenInclude(od => od.Product)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                claimsQuery = claimsQuery.Where(wc =>
                    wc.ClaimCode.Contains(searchTerm) ||
                    wc.Warranty.WarrantyCode.Contains(searchTerm) ||
                    wc.Warranty.Customer.Name.Contains(searchTerm) ||
                    wc.IssueDescription.Contains(searchTerm));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                claimsQuery = claimsQuery.Where(wc => wc.Status == status);
            }

            var totalClaims = await claimsQuery.CountAsync();
            var claims = await claimsQuery
                .OrderByDescending(wc => wc.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new WarrantyClaimListViewModel
            {
                Claims = claims,
                SearchTerm = searchTerm,
                Status = status,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalClaims / pageSize),
                TotalClaims = totalClaims
            };

            ViewBag.StatusList = WarrantyClaim.ClaimStatus.AllStatuses;

            return View(viewModel);
        }

        // GET: Warranty/ClaimDetails/5
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _context.WarrantyClaims
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.Customer)
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.OrderDetail)
                        .ThenInclude(od => od.Product)
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.OrderDetail)
                        .ThenInclude(od => od.Color)
                .FirstOrDefaultAsync(wc => wc.WarrantyClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            ViewBag.StatusList = WarrantyClaim.ClaimStatus.AllStatuses;
            ViewBag.ResolutionTypes = WarrantyClaim.ResolutionTypes.AllTypes;

            return View(claim);
        }

        // POST: Warranty/ProcessClaim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessClaim(int claimId, string status, string adminNotes, 
            string resolution, string resolutionType)
        {
            var claim = await _context.WarrantyClaims.FindAsync(claimId);
            if (claim == null)
            {
                return NotFound();
            }

            var adminUsername = User.Identity?.Name ?? "Admin";

            claim.Status = status;
            claim.AdminNotes = adminNotes;
            claim.Resolution = resolution;
            claim.ResolutionType = resolutionType;
            claim.ProcessedDate = DateTime.Now;
            claim.ProcessedByAdmin = adminUsername;

            if (status == WarrantyClaim.ClaimStatus.Completed)
            {
                claim.CompletedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Xử lý yêu cầu bảo hành thành công!";
            return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
        }

        // POST: Warranty/UpdateClaimStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimStatus(int claimId, string status)
        {
            try
            {
                var claim = await _context.WarrantyClaims.FindAsync(claimId);
                if (claim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khiếu nại" });
                }

                claim.Status = status;
                claim.ProcessedDate = DateTime.Now;
                claim.ProcessedByAdmin = HttpContext.Session.GetString("AdminName") ?? "Admin";

                if (status == WarrantyClaim.ClaimStatus.Completed)
                {
                    claim.CompletedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim status for claim {ClaimId}", claimId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái" });
            }
        }

        // Tạo mã bảo hành duy nhất
        private string GenerateWarrantyCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"WR{timestamp}{random}";
        }

        // API: Tạo bảo hành tự động khi đơn hàng hoàn thành
        [HttpPost]
        public async Task<IActionResult> AutoCreateWarranty(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.Status != Order.OrderStatus.Delivered)
                {
                    return Json(new { success = false, message = "Đơn hàng chưa hoàn thành" });
                }

                var warrantyCount = 0;

                foreach (var orderDetail in order.OrderDetails)
                {
                    // Kiểm tra đã có bảo hành chưa
                    var existingWarranty = await _context.Warranties
                        .AnyAsync(w => w.OrderDetailId == orderDetail.OrderDetailId);

                    if (!existingWarranty && orderDetail.Product.WarrantyPeriodMonths > 0)
                    {
                        var warranty = new Warranty
                        {
                            OrderDetailId = orderDetail.OrderDetailId,
                            CustomerId = order.CustomerId ?? 0,
                            WarrantyCode = GenerateWarrantyCode(),
                            StartDate = DateTime.Now,
                            WarrantyPeriodMonths = orderDetail.Product.WarrantyPeriodMonths,
                            EndDate = DateTime.Now.AddMonths(orderDetail.Product.WarrantyPeriodMonths),
                            Status = Warranty.WarrantyStatus.Active,
                            CreatedDate = DateTime.Now
                        };

                        _context.Warranties.Add(warranty);
                        warrantyCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Đã tạo {warrantyCount} bảo hành tự động",
                    count = warrantyCount 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auto warranty for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo bảo hành" });
            }
        }
    }
}
