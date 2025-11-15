using System.Diagnostics;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAzureStorageService _storageService;

        public HomeController(ILogger<HomeController> logger, IAzureStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole == "Admin")
            {
                return RedirectToAction("AdminDashboard");
            }
            if (!string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("CustomerDashboard");
            }

            try
            {
                var dashboardData = await _storageService.GetDashboardDataAsync();

                // Check storage status for the view
                try
                {
                    var isStorageInitialized = await _storageService.IsStorageInitializedAsync();
                    ViewBag.IsStorageInitialized = isStorageInitialized;

                    // Add detailed logging to debug the issue
                    _logger.LogInformation($"Home page - Storage initialization check result: {isStorageInitialized}");
                    Console.WriteLine($"Home page - Storage initialization check result: {isStorageInitialized}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not determine storage initialization status");
                    ViewBag.IsStorageInitialized = false;
                    Console.WriteLine($"Home page - Error checking storage status: {ex.Message}");
                }

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return View(new HomeViewModel());
            }
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

        // POST: Initialize Azure Storage
        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                _logger.LogInformation("Starting Azure Storage initialization...");

                // Force a fresh initialization by calling the service
                // This will create all necessary tables, containers, queues, and file shares
                await _storageService.InitializeStorageManuallyAsync();

                _logger.LogInformation("Azure Storage initialized successfully!");
                TempData["Success"] = "Azure Storage initialized successfully! All storage resources have been created.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Azure Storage");
                TempData["Error"] = $"Error initializing Azure Storage: {ex.Message}. Please check your connection string and try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Debug Storage Status
        public async Task<IActionResult> DebugStorage()
        {
            try
            {
                var isStorageInitialized = await _storageService.IsStorageInitializedAsync();
                var result = new
                {
                    IsStorageInitialized = isStorageInitialized,
                    Timestamp = DateTime.UtcNow,
                    Message = isStorageInitialized ? "Storage is ready" : "Storage is not ready"
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                var result = new
                {
                    IsStorageInitialized = false,
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message,
                    Message = "Error checking storage status"
                };

                return Json(result);
            }
        }

        // GET: Home/AdminDashboard
        public async Task<IActionResult> AdminDashboard()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            try
            {
                var dashboardData = await _storageService.GetDashboardDataAsync();
                ViewBag.UserName = HttpContext.Session.GetString("UserName");
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data");
                return View(new HomeViewModel());
            }
        }

        // GET: Home/CustomerDashboard
        public async Task<IActionResult> CustomerDashboard()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var dashboardData = await _storageService.GetDashboardDataAsync();
                ViewBag.UserName = HttpContext.Session.GetString("UserName");
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer dashboard data");
                return View(new HomeViewModel());
            }
        }

        // GET: Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
