using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<CustomerController> _logger;
        private readonly bool _useFunctions;

        public CustomerController(IAzureStorageService storageService, IFunctionsApi functionsApi, ILogger<CustomerController> logger, IConfiguration configuration)
        {
            _storageService = storageService;
            _functionsApi = functionsApi;
            _logger = logger;
            _useFunctions = configuration.GetValue<bool>("UseFunctions");
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            try
            {
                var customers = _useFunctions
                    ? await _functionsApi.GetCustomersAsync()
                    : await _storageService.GetAllEntitiesAsync<Customer>("customers");
                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                TempData["Error"] = "Error retrieving customers. Please try again.";
                return View(new List<Customer>());
            }
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (_useFunctions)
                        await _functionsApi.CreateCustomerAsync(customer);
                    else
                        await _storageService.CreateCustomerAsync(customer);

                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer");
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var customer = _useFunctions
                    ? await _functionsApi.GetCustomerAsync(id)
                    : await _storageService.GetEntityAsync<Customer>("customers", id);

                if (customer == null)
                {
                    return NotFound();
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for edit");
                TempData["Error"] = "Error retrieving customer. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (_useFunctions)
                        await _functionsApi.UpdateCustomerAsync(customer);
                    else
                        await _storageService.UpdateCustomerAsync(customer);

                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating customer");
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (_useFunctions)
                    await _functionsApi.DeleteCustomerAsync(id);
                else
                    await _storageService.DeleteEntityAsync<Customer>("customers", id);

                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}