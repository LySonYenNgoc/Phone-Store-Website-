using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Attributes;
using PhoneStore.Models;
using PhoneStore.ViewModels;

namespace PhoneStore.Controllers
{
    [AdminAuthorize]
    public class CustomerController : Controller
    {
        private readonly PhoneStoreContext _context;

        public CustomerController(PhoneStoreContext context)
        {
            _context = context;
        }

        // GET: Customer
        public async Task<IActionResult> Index(string searchString, int? membershipId, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Customers
                .Include(c => c.Membership)
                .AsQueryable();

            // Áp dụng tìm kiếm nếu có
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c =>
                    c.Name != null && c.Name.Contains(searchString) ||
                    c.Email != null && c.Email.Contains(searchString) ||
                    c.Phone != null && c.Phone.Contains(searchString));
            }

            // Lọc theo loại thành viên
            if (membershipId.HasValue)
            {
                query = query.Where(c => c.MembershipId == membershipId);
            }

            // Tính tổng số khách hàng theo bộ lọc
            int totalCustomers = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCustomers / (double)pageSize);

            // Điều chỉnh page nếu vượt quá giới hạn
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Lấy dữ liệu trang hiện tại
            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy danh sách loại thành viên
            var memberships = await _context.Memberships.ToListAsync();
            ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name");

            // Tạo ViewModel
            var viewModel = new CustomerFilterViewModel
            {
                SearchString = searchString,
                MembershipId = membershipId,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Customers = customers,
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = await _context.Customers
                    .Where(c => c.CreatedDate != null && c.CreatedDate.Value.Month == DateTime.Now.Month && c.CreatedDate.Value.Year == DateTime.Now.Year)
                    .CountAsync(),
                CustomersWithOrders = await _context.Customers
                    .Where(c => c.Orders.Any())
                    .CountAsync()
            };

            return View(viewModel);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.Membership)
                .Include(c => c.Orders)
                .Include(c => c.ShippingAddresses)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Lấy thêm thông tin đơn hàng
            var orders = await _context.Orders
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = orders;

            // Thống kê chi tiêu
            decimal totalSpent = orders.Sum(o => o.TotalAmount ?? 0);
            ViewBag.TotalSpent = totalSpent;
            ViewBag.OrderCount = orders.Count;
            ViewBag.AverageOrderValue = orders.Count > 0 ? totalSpent / orders.Count : 0;

            // Sản phẩm đã mua
            var purchasedProductIds = await _context.OrderDetails
                .Where(od => od.Order != null && od.Order.CustomerId == id)
                .Select(od => od.ProductId)
                .Distinct()
                .ToListAsync();

            var purchasedProducts = await _context.Products
                .Where(p => purchasedProductIds.Contains(p.ProductId))
                .ToListAsync();

            ViewBag.PurchasedProducts = purchasedProducts;

            return View(customer);
        }

        // GET: Customer/Create
        public async Task<IActionResult> Create()
        {
            var memberships = await _context.Memberships.ToListAsync();
            ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name");
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,BirthYear,MembershipId")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email hoặc số điện thoại đã tồn tại chưa
                if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    var memberships = await _context.Memberships.ToListAsync();
                    ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name");
                    return View(customer);
                }

                if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng");
                    var memberships = await _context.Memberships.ToListAsync();
                    ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name");
                    return View(customer);
                }

                customer.CreatedDate = DateTime.Now;
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var membershipsList = await _context.Memberships.ToListAsync();
            ViewBag.Memberships = new SelectList(membershipsList, "MembershipId", "Name");
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var memberships = await _context.Memberships.ToListAsync();
            ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name", customer.MembershipId);

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,Name,Email,Phone,BirthYear,MembershipId")] Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra email hoặc số điện thoại đã tồn tại chưa (trừ khách hàng hiện tại)
                if (await _context.Customers.AnyAsync(c => c.Email == customer.Email && c.CustomerId != id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    var memberships = await _context.Memberships.ToListAsync();
                    ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name", customer.MembershipId);
                    return View(customer);
                }

                if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone && c.CustomerId != id))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng");
                    var memberships = await _context.Memberships.ToListAsync();
                    ViewBag.Memberships = new SelectList(memberships, "MembershipId", "Name", customer.MembershipId);
                    return View(customer);
                }

                try
                {
                    // Lấy thông tin hiện tại từ database
                    var existingCustomer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == id);
                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Giữ lại một số thông tin không thay đổi
                    customer.CreatedDate = existingCustomer.CreatedDate;
                    customer.PasswordHash = existingCustomer.PasswordHash;

                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var membershipsList = await _context.Memberships.ToListAsync();
            ViewBag.Memberships = new SelectList(membershipsList, "MembershipId", "Name", customer.MembershipId);
            return View(customer);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.Membership)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Kiểm tra xem khách hàng có đơn hàng không
            var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
            ViewBag.HasOrders = hasOrders;

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Kiểm tra xem khách hàng có đơn hàng không
            var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
            if (hasOrders)
            {
                TempData["Error"] = "Không thể xóa khách hàng này vì đã có đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa địa chỉ giao hàng của khách hàng
            var shippingAddresses = await _context.ShippingAddresses
                .Where(sa => sa.CustomerId == id)
                .ToListAsync();

            _context.ShippingAddresses.RemoveRange(shippingAddresses);

            // Xóa khách hàng
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã xóa khách hàng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Report
        public async Task<IActionResult> Report()
        {
            // Thống kê tổng số khách hàng theo tháng
            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var customersByMonth = await _context.Customers
                .Where(c => c.CreatedDate != null && c.CreatedDate >= sixMonthsAgo)
                .GroupBy(c => new { Month = c.CreatedDate!.Value.Month, Year = c.CreatedDate.Value.Year })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Count = g.Count()
                })
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            // Chuyển đổi dữ liệu cho biểu đồ
            var months = new List<string>();
            var counts = new List<int>();

            // Lấy 6 tháng gần nhất
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var monthName = date.ToString("MM/yyyy");
                months.Add(monthName);

                var monthData = customersByMonth
                    .FirstOrDefault(r => r.Month == date.Month && r.Year == date.Year);

                counts.Add(monthData?.Count ?? 0);
            }

            // Thống kê khách hàng theo loại thành viên
            var customersByMembership = await _context.Customers
                .GroupBy(c => c.MembershipId ?? 0)
                .Select(g => new
                {
                    MembershipId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var memberships = await _context.Memberships.ToListAsync();
            var membershipData = customersByMembership.Select(cm => new
            {
                MembershipName = memberships.FirstOrDefault(m => m.MembershipId == cm.MembershipId)?.Name ?? "Không có",
                Count = cm.Count
            }).ToList();

            // Thống kê khách hàng theo năm sinh
            var customersByBirthYear = await _context.Customers
                .Where(c => c.BirthYear != null)
                .GroupBy(c => c.BirthYear!.Value)
                .Select(g => new
                {
                    BirthYear = g.Key,
                    Count = g.Count()
                })
                .OrderBy(c => c.BirthYear)
                .ToListAsync();

            // Phân loại theo thập kỷ
            var customersByDecade = customersByBirthYear
                .GroupBy(c => (c.BirthYear / 10) * 10)
                .Select(g => new
                {
                    Decade = g.Key,
                    Count = g.Sum(c => c.Count)
                })
                .OrderBy(c => c.Decade)
                .ToList();

            var decades = customersByDecade.Select(d => $"{d.Decade}s").ToList();
            var decadeCounts = customersByDecade.Select(d => d.Count).ToList();

            // Thống kê top khách hàng theo giá trị đơn hàng
            var topCustomers = await _context.Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount ?? 0),
                    OrderCount = g.Count()
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToListAsync();

            var customerIds = topCustomers.Select(c => c.CustomerId).ToList();
            var customerInfos = await _context.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync();

            var topCustomersData = topCustomers.Select(tc => new
            {
                Customer = customerInfos.FirstOrDefault(c => c.CustomerId == tc.CustomerId),
                TotalSpent = tc.TotalSpent,
                OrderCount = tc.OrderCount
            }).ToList();

            // Thống kê khách hàng không có đơn hàng
            var customersWithoutOrders = await _context.Customers
                .Where(c => !c.Orders.Any())
                .CountAsync();

            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.CustomersWithoutOrders = customersWithoutOrders;

            ViewBag.ChartMonths = string.Join(",", months.Select(m => $"'{m}'"));
            ViewBag.ChartCounts = string.Join(",", counts);

            ViewBag.MembershipData = membershipData;

            ViewBag.Decades = string.Join(",", decades.Select(d => $"'{d}'"));
            ViewBag.DecadeCounts = string.Join(",", decadeCounts);

            ViewBag.TopCustomers = topCustomersData;

            return View();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}
