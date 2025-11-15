using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Login/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Login/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username already exists
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists. Please choose a different username.");
                        return View(model);
                    }

                    // Check if email already exists
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists. Please use a different email.");
                        return View(model);
                    }

                    // Hash password (simple hash - in production, use proper password hashing like BCrypt or ASP.NET Identity)
                    var hashedPassword = HashPassword(model.Password);

                    var normalizedRole = string.Equals(model.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                        ? "Admin"
                        : "Customer";

                    // Create new user
                    var user = new User
                    {
                        Username = model.Username,
                        Password = hashedPassword,
                        Name = model.Name,
                        Surname = model.Surname,
                        Email = model.Email,
                        ShippingAddress = model.ShippingAddress ?? string.Empty,
                        Role = normalizedRole,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Registration successful! Please login.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error registering user");
                    ModelState.AddModelError("", $"Error during registration: {ex.Message}");
                }
            }
            return View(model);
        }

        // GET: Login/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var normalizedRole = model.SelectedRole?.Trim();
                    if (string.IsNullOrEmpty(normalizedRole))
                    {
                        ModelState.AddModelError("SelectedRole", "Please select a role.");
                        return View(model);
                    }

                    var hashedPassword = HashPassword(model.Password);
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username &&
                                                   u.Password == hashedPassword &&
                                                   u.Role == normalizedRole);

                    if (user != null)
                    {
                        // Store user info in session
                        HttpContext.Session.SetString("UserId", user.UserId.ToString());
                        HttpContext.Session.SetString("Username", user.Username);
                        HttpContext.Session.SetString("UserRole", user.Role);
                        HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");

                        TempData["Success"] = $"Welcome back, {user.Name}!";

                        // Redirect based on role
                        if (user.Role == "Admin")
                        {
                            return RedirectToAction("AdminDashboard", "Home");
                        }
                        else
                        {
                            return RedirectToAction("CustomerDashboard", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid username, password, or role.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login");
                    ModelState.AddModelError("", $"Error during login: {ex.Message}");
                }
            }
            return View(model);
        }

        // GET: Login/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login", "Login");
        }

        // Helper method to hash password (simple SHA256 - in production use proper hashing)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

