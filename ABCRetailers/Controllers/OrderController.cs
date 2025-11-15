using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<OrderController> _logger;
        private readonly bool _useFunctions;

        public OrderController(IAzureStorageService storageService, IFunctionsApi functionsApi, ILogger<OrderController> logger, IConfiguration configuration)
        {
            _storageService = storageService;
            _functionsApi = functionsApi;
            _logger = logger;
            _useFunctions = configuration.GetValue<bool>("UseFunctions");
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login to view orders.";
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var allOrders = _useFunctions
                    ? await _functionsApi.GetOrdersAsync()
                    : await _storageService.GetAllEntitiesAsync<Order>("orders");

                // Filter orders based on user role
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole == "Admin")
                {
                    // Admin sees all orders
                    return View(allOrders);
                }
                else
                {
                    // Customer sees only their own orders
                    var customerOrders = allOrders.Where(o => o.CustomerId == userId).ToList();
                    return View(customerOrders);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                TempData["Error"] = "Error retrieving orders. Please try again.";
                return View(new List<Order>());
            }
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var customers = await _storageService.GetAllEntitiesAsync<Customer>("customers");
                var products = await _storageService.GetAllEntitiesAsync<Product>("products");

                var viewModel = new OrderCreateViewModel
                {
                    Customers = customers,
                    Products = products
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing order creation");
                TempData["Error"] = "Error preparing order creation. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("customers", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("products", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Create order
                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted" // Always starts as Submitted
                    };

                    if (_useFunctions)
                    {
                        // Send to queue via Functions (queue trigger will process it)
                        await _functionsApi.CreateOrderAsync(order);
                    }
                    else
                    {
                        await _storageService.CreateOrderAsync(order);
                    }

                    // Update product stock
                    product.StockAvailable -= model.Quantity;
                    if (_useFunctions)
                        await _functionsApi.UpdateProductAsync(product, null);
                    else
                        await _storageService.UpdateEntityAsync(product);

                    // Send queue message for new order
                    var orderMessage = new
                    {
                        order.OrderId,
                        order.CustomerId,
                        CustomerName = $"{customer.Name} {customer.Surname}",
                        order.ProductName,
                        order.Quantity,
                        order.TotalPrice,
                        order.OrderDate,
                        order.Status
                    };
                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));

                    // Send stock update message
                    var stockMessage = new
                    {
                        product.ProductId,
                        product.ProductName,
                        PreviousStock = product.StockAvailable + model.Quantity,
                        NewStock = product.StockAvailable,
                        UpdatedBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };
                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating order");
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: Order/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var order = _useFunctions
                    ? await _functionsApi.GetOrderAsync(id)
                    : await _storageService.GetEntityAsync<Order>("orders", id);

                if (order == null)
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order for edit");
                TempData["Error"] = "Error retrieving order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure OrderDate is UTC before saving
                    order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);

                    if (_useFunctions)
                        await _functionsApi.UpdateOrderAsync(order);
                    else
                        await _storageService.UpdateEntityAsync(order);

                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating order");
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var order = _useFunctions
                    ? await _functionsApi.GetOrderAsync(id)
                    : await _storageService.GetEntityAsync<Order>("orders", id);

                if (order == null)
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details");
                TempData["Error"] = "Error retrieving order details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (_useFunctions)
                    await _functionsApi.DeleteOrderAsync(id);
                else
                    await _storageService.DeleteEntityAsync<Order>("orders", id);

                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get product price for real-time calculation
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("products", productId);
                if (product != null)
                {
                    return Json(new { success = true, price = product.Price, stock = product.StockAvailable, productName = product.ProductName });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product price");
                return Json(new { success = false });
            }
        }

        // AJAX: Update order status
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request data" });
            }

            var id = request.Id;
            var newStatus = request.NewStatus;

            try
            {
                var order = _useFunctions
                    ? await _functionsApi.GetOrderAsync(id)
                    : await _storageService.GetEntityAsync<Order>("orders", id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var previousStatus = order.Status;
                order.Status = newStatus;

                if (_useFunctions)
                    await _functionsApi.UpdateOrderAsync(order);
                else
                    await _storageService.UpdateOrderAsync(order);

                // Send queue message for status update
                var statusMessage = new
                {
                    order.OrderId,
                    order.CustomerId,
                    CustomerName = order.Username,
                    order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };
                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Order/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login to view your orders.";
                return RedirectToAction("Login", "Login");
            }

            try
            {
                var allOrders = _useFunctions
                    ? await _functionsApi.GetOrdersAsync()
                    : await _storageService.GetAllEntitiesAsync<Order>("orders");

                // Customer sees only their own orders
                var customerOrders = allOrders.Where(o => o.CustomerId == userId).OrderByDescending(o => o.OrderDate).ToList();
                return View(customerOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer orders");
                TempData["Error"] = "Error retrieving orders. Please try again.";
                return View(new List<Order>());
            }
        }

        // GET: Order/Manage (Admin only)
        public async Task<IActionResult> Manage()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            try
            {
                var orders = _useFunctions
                    ? await _functionsApi.GetOrdersAsync()
                    : await _storageService.GetAllEntitiesAsync<Order>("orders");
                return View("ManageOrders", orders.OrderByDescending(o => o.OrderDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for management");
                TempData["Error"] = "Error retrieving orders. Please try again.";
                return View(new List<Order>());
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>("customers");
            model.Products = await _storageService.GetAllEntitiesAsync<Product>("products");
        }
    }
}