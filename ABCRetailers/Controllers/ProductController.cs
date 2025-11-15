using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<ProductController> _logger;
        private readonly bool _useFunctions;

        public ProductController(IAzureStorageService storageService, IFunctionsApi functionsApi, ILogger<ProductController> logger, IConfiguration configuration)
        {
            _storageService = storageService;
            _functionsApi = functionsApi;
            _logger = logger;
            _useFunctions = configuration.GetValue<bool>("UseFunctions");
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = _useFunctions
                    ? await _functionsApi.GetProductsAsync()
                    : await _storageService.GetAllEntitiesAsync<Product>("products");
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                TempData["Error"] = "Error retrieving products. Please try again.";
                return View(new List<Product>());
            }
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Manual price parsing to fix binding issue
                    if (Request.Form.TryGetValue("PriceString", out var priceFormValue))
                    {
                        _logger.LogInformation("Raw price from form: {PriceFormValue}", priceFormValue);
                        if (double.TryParse(priceFormValue, out double parsedPrice))
                        {
                            product.Price = parsedPrice;
                            _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                        }
                        else
                        {
                            _logger.LogWarning("Could not parse PriceFormValue: {PriceFormValue}", priceFormValue);
                        }
                    }
                    _logger.LogInformation("Final product price: {Price}", product.Price);

                    // Custom price validation
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("", "Price must be greater than $0.00");
                        return View(product);
                    }

                    if (_useFunctions)
                    {
                        // Functions handles image upload internally
                        await _functionsApi.CreateProductAsync(product, imageFile);
                    }
                    else
                    {
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            var imageUrl = await _storageService.UploadImageAsync(imageFile, "productimages");
                            product.ImageUrl = imageUrl;
                        }
                        await _storageService.CreateProductAsync(product);
                    }

                    TempData["Success"] = $"Product {product.ProductName} created successfully with price {product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var product = _useFunctions
                    ? await _functionsApi.GetProductAsync(id)
                    : await _storageService.GetEntityAsync<Product>("products", id);

                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for edit");
                TempData["Error"] = "Error retrieving product. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Manual price parsing for edit too
                    if (Request.Form.TryGetValue("PriceString", out var priceFormValue))
                    {
                        if (double.TryParse(priceFormValue, out double parsedPrice))
                        {
                            product.Price = parsedPrice;
                            _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                        }
                    }

                    if (_useFunctions)
                    {
                        // Functions handles image upload and update
                        await _functionsApi.UpdateProductAsync(product, imageFile);
                    }
                    else
                    {
                        // Get the original product to preserve ETag
                        var originalProduct = await _storageService.GetEntityAsync<Product>("products", product.RowKey);
                        if (originalProduct == null)
                        {
                            return NotFound();
                        }

                        // Update properties but keep the original ETag
                        originalProduct.ProductName = product.ProductName;
                        originalProduct.Description = product.Description;
                        originalProduct.Price = product.Price;
                        originalProduct.StockAvailable = product.StockAvailable;

                        if (imageFile != null && imageFile.Length > 0)
                        {
                            var imageUrl = await _storageService.UploadImageAsync(imageFile, "productimages");
                            originalProduct.ImageUrl = imageUrl;
                        }

                        await _storageService.UpdateEntityAsync(originalProduct);
                    }

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (_useFunctions)
                    await _functionsApi.DeleteProductAsync(id);
                else
                    await _storageService.DeleteEntityAsync<Product>("products", id);

                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}