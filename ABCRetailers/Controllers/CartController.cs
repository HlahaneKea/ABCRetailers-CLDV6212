using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Data;
using ABCRetailers.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABCRetailers.Controllers
{
    public class CartController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<CartController> _logger;
        private readonly bool _useFunctions;

        public CartController(
            AuthDbContext context,
            IAzureStorageService storageService,
            IFunctionsApi functionsApi,
            ILogger<CartController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _storageService = storageService;
            _functionsApi = functionsApi;
            _logger = logger;
            _useFunctions = configuration.GetValue<bool>("UseFunctions");
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login to view your cart.";
                return RedirectToAction("Login", "Login");
            }

            var userIdInt = int.Parse(userId);
            var cartItems = await _context.Carts
                .Where(c => c.UserId == userIdInt)
                .ToListAsync();

            decimal total = 0;
            foreach (var item in cartItems)
            {
                total += item.TotalPrice;
            }

            ViewBag.CartTotal = total;
            return View(cartItems);
        }

        [HttpGet]
        public IActionResult Confirmation()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                TempData["Error"] = "Please login to view order confirmation.";
                return RedirectToAction("Login", "Login");
            }

            if (TempData["Success"] == null)
            {
                TempData["Success"] = "Order placed successfully!";
            }

            return View();
        }

        // POST: Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to add items to cart." });
            }

            try
            {
                // Get product from Azure Storage
                Product? product = null;
                if (_useFunctions)
                {
                    product = await _functionsApi.GetProductAsync(productId);
                }
                else
                {
                    product = await _storageService.GetEntityAsync<Product>("products", productId);
                }

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Check stock
                if (product.StockAvailable < quantity)
                {
                    return Json(new { success = false, message = $"Insufficient stock. Available: {product.StockAvailable}" });
                }

                var userIdInt = int.Parse(userId);

                // Check if item already in cart
                var existingCartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    // Update quantity
                    existingCartItem.Quantity += quantity;
                    existingCartItem.TotalPrice = existingCartItem.Quantity * existingCartItem.UnitPrice;
                }
                else
                {
                    // Add new item
                    var cartItem = new Cart
                    {
                        UserId = userIdInt,
                        ProductId = productId,
                        ProductName = product.ProductName,
                        Quantity = quantity,
                        UnitPrice = (decimal)product.Price,
                        TotalPrice = (decimal)product.Price * quantity,
                        AddedDate = DateTime.UtcNow
                    };
                    _context.Carts.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Item added to cart successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartId, int quantity)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login." });
            }

            try
            {
                var userIdInt = int.Parse(userId);
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userIdInt);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found." });
                }

                // Check stock
                Product? product = null;
                if (_useFunctions)
                {
                    product = await _functionsApi.GetProductAsync(cartItem.ProductId);
                }
                else
                {
                    product = await _storageService.GetEntityAsync<Product>("products", cartItem.ProductId);
                }

                if (product != null && product.StockAvailable < quantity)
                {
                    return Json(new { success = false, message = $"Insufficient stock. Available: {product.StockAvailable}" });
                }

                cartItem.Quantity = quantity;
                cartItem.TotalPrice = cartItem.UnitPrice * quantity;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cart updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login." });
            }

            try
            {
                var userIdInt = int.Parse(userId);
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userIdInt);

                if (cartItem != null)
                {
                    _context.Carts.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Item removed from cart." });
                }

                return Json(new { success = false, message = "Cart item not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login to checkout.";
                return RedirectToAction("Login", "Login");
            }

            var userIdInt = int.Parse(userId);
            var cartItems = await _context.Carts
                .Where(c => c.UserId == userIdInt)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userIdInt);
            ViewBag.User = user;

            return View(cartItems);
        }

        // POST: Cart/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login to process order.";
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var userIdInt = int.Parse(userId);
                var cartItems = await _context.Carts
                    .Where(c => c.UserId == userIdInt)
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Create orders for each cart item
                foreach (var cartItem in cartItems)
                {
                    // Get product to verify stock
                    Product? product = null;
                    if (_useFunctions)
                    {
                        product = await _functionsApi.GetProductAsync(cartItem.ProductId);
                    }
                    else
                    {
                        product = await _storageService.GetEntityAsync<Product>("products", cartItem.ProductId);
                    }

                    if (product == null || product.StockAvailable < cartItem.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {cartItem.ProductName}.";
                        return RedirectToAction("Index");
                    }

                    // Create order
                    var order = new Order
                    {
                        CustomerId = user.UserId.ToString(),
                        Username = user.Username,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        OrderDate = DateTime.UtcNow,
                        Quantity = cartItem.Quantity,
                        UnitPrice = (double)cartItem.UnitPrice,
                        TotalPrice = (double)cartItem.TotalPrice,
                        Status = "Submitted"
                    };

                    if (_useFunctions)
                    {
                        await _functionsApi.CreateOrderAsync(order);
                    }
                    else
                    {
                        await _storageService.CreateOrderAsync(order);
                    }

                    // Update product stock
                    product.StockAvailable -= cartItem.Quantity;
                    if (_useFunctions)
                    {
                        await _functionsApi.UpdateProductAsync(product, null);
                    }
                    else
                    {
                        await _storageService.UpdateEntityAsync(product);
                    }
                }

                // Clear cart
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction("Confirmation", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                TempData["Error"] = $"Error processing order: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}

