using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Customer.Models;
using PhoneStore.Customer.ViewModels;

namespace PhoneStore.Customer.Controllers
{
    public class WarrantyController : Controller
    {
        private readonly PhoneStoreContext _context;
        private readonly ILogger<WarrantyController> _logger;

        public WarrantyController(PhoneStoreContext context, ILogger<WarrantyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Warranty - Xem danh sách bảo hành của khách hàng
        public async Task<IActionResult> Index()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var warranties = await _context.Warranties
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Color)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(w => w.WarrantyClaims)
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();

            var claims = await _context.WarrantyClaims
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.OrderDetail)
                        .ThenInclude(od => od.Product)
                .Where(wc => wc.Warranty.CustomerId == customerId)
                .OrderByDescending(wc => wc.SubmittedDate)
                .ToListAsync();

            var viewModel = new CustomerWarrantyViewModel
            {
                Warranties = warranties,
                Claims = claims,
                ActiveWarranties = warranties.Count(w => w.IsActiveWarranty()),
                ExpiredWarranties = warranties.Count(w => w.Status == Warranty.WarrantyStatus.Expired || DateTime.Now > w.EndDate),
                PendingClaims = claims.Count(c => c.Status == WarrantyClaim.ClaimStatus.Pending)
            };

            return View(viewModel);
        }

        // GET: Warranty/Details/5 - Xem chi tiết bảo hành
        public async Task<IActionResult> Details(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var warranty = await _context.Warranties
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Color)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(w => w.WarrantyClaims)
                .FirstOrDefaultAsync(w => w.WarrantyId == id && w.CustomerId == customerId);

            if (warranty == null)
            {
                return NotFound();
            }

            return View(warranty);
        }

        // GET: Warranty/CreateClaim/5 - Tạo yêu cầu bảo hành
        public async Task<IActionResult> CreateClaim(int warrantyId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var warranty = await _context.Warranties
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Color)
                .FirstOrDefaultAsync(w => w.WarrantyId == warrantyId && w.CustomerId == customerId);

            if (warranty == null)
            {
                return NotFound();
            }

            if (!warranty.IsActiveWarranty())
            {
                TempData["Error"] = "Bảo hành đã hết hạn hoặc không còn hiệu lực!";
                return RedirectToAction(nameof(Details), new { id = warrantyId });
            }

            var viewModel = new CreateWarrantyClaimViewModel
            {
                WarrantyId = warrantyId,
                Warranty = warranty
            };

            ViewBag.IssueTypes = WarrantyClaim.ClaimIssueType.AllTypes;

            return View(viewModel);
        }

        // POST: Warranty/CreateClaim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(CreateWarrantyClaimViewModel model)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra warranty còn hiệu lực
                var warranty = await _context.Warranties
                    .FirstOrDefaultAsync(w => w.WarrantyId == model.WarrantyId && w.CustomerId == customerId);

                if (warranty == null || !warranty.IsActiveWarranty())
                {
                    TempData["Error"] = "Bảo hành không tồn tại hoặc đã hết hạn!";
                    return RedirectToAction(nameof(Index));
                }

                var claim = new WarrantyClaim
                {
                    WarrantyId = model.WarrantyId,
                    ClaimCode = GenerateClaimCode(),
                    IssueDescription = model.IssueDescription,
                    IssueType = model.IssueType,
                    Status = WarrantyClaim.ClaimStatus.Pending,
                    SubmittedDate = DateTime.Now
                };

                _context.WarrantyClaims.Add(claim);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã gửi yêu cầu bảo hành thành công! Mã yêu cầu: {claim.ClaimCode}";
                return RedirectToAction(nameof(ClaimDetails), new { id = claim.WarrantyClaimId });
            }

            // Reload warranty if validation fails
            model.Warranty = await _context.Warranties
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Color)
                .FirstOrDefaultAsync(w => w.WarrantyId == model.WarrantyId);

            ViewBag.IssueTypes = WarrantyClaim.ClaimIssueType.AllTypes;

            return View(model);
        }

        // GET: Warranty/ClaimDetails/5 - Xem chi tiết yêu cầu bảo hành
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var claim = await _context.WarrantyClaims
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.OrderDetail)
                        .ThenInclude(od => od.Product)
                .Include(wc => wc.Warranty)
                    .ThenInclude(w => w.OrderDetail)
                        .ThenInclude(od => od.Color)
                .FirstOrDefaultAsync(wc => wc.WarrantyClaimId == id && wc.Warranty.CustomerId == customerId);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // GET: Warranty/CheckWarranty - Kiểm tra bảo hành bằng mã
        public IActionResult CheckWarranty()
        {
            return View();
        }

        // POST: Warranty/CheckWarranty
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckWarranty(string warrantyCode)
        {
            if (string.IsNullOrEmpty(warrantyCode))
            {
                TempData["Error"] = "Vui lòng nhập mã bảo hành!";
                return View();
            }

            var warranty = await _context.Warranties
                .Include(w => w.Customer)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Color)
                .Include(w => w.WarrantyClaims)
                .FirstOrDefaultAsync(w => w.WarrantyCode == warrantyCode);

            if (warranty == null)
            {
                TempData["Error"] = "Không tìm thấy bảo hành với mã này!";
                return View();
            }

            ViewBag.WarrantyCode = warrantyCode;
            return View("CheckWarrantyResult", warranty);
        }

        // Tạo mã yêu cầu bảo hành
        private string GenerateClaimCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"WC{timestamp}{random}";
        }

        // API: Lấy thông tin bảo hành
        [HttpGet]
        public async Task<IActionResult> GetWarrantyInfo(int warrantyId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var warranty = await _context.Warranties
                .Include(w => w.OrderDetail)
                    .ThenInclude(od => od.Product)
                .Where(w => w.WarrantyId == warrantyId && w.CustomerId == customerId)
                .Select(w => new
                {
                    w.WarrantyId,
                    w.WarrantyCode,
                    w.Status,
                    w.StartDate,
                    w.EndDate,
                    DaysRemaining = w.DaysRemaining(),
                    IsActive = w.IsActiveWarranty(),
                    Product = new
                    {
                        w.OrderDetail.Product.ProductName,
                        w.OrderDetail.Product.Price
                    }
                })
                .FirstOrDefaultAsync();

            if (warranty == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bảo hành" });
            }

            return Json(new { success = true, data = warranty });
        }
    }
}
