using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Models;

using PhoneStore.Attributes;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PhoneStore.Controllers;

[AdminAuthorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }    public IActionResult Index()
    {
        // Chuyển hướng về trang Dashboard
        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
