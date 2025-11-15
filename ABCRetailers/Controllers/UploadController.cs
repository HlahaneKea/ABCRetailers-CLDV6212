using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<UploadController> _logger;
        private readonly bool _useFunctions;

        public UploadController(IAzureStorageService storageService, IFunctionsApi functionsApi, ILogger<UploadController> logger, IConfiguration configuration)
        {
            _storageService = storageService;
            _functionsApi = functionsApi;
            _logger = logger;
            _useFunctions = configuration.GetValue<bool>("UseFunctions");
        }

        // GET: Upload
        public async Task<IActionResult> Index()
        {
            try
            {
                var isStorageInitialized = await _storageService.IsStorageInitializedAsync();
                ViewBag.IsStorageInitialized = isStorageInitialized;

                // Add detailed logging to debug the issue
                _logger.LogInformation($"Upload page - Storage initialization check result: {isStorageInitialized}");
                Console.WriteLine($"Upload page - Storage initialization check result: {isStorageInitialized}");
            }
            catch (Exception ex)
            {
                // If we can't check storage status, assume it's not initialized
                _logger.LogWarning(ex, "Could not determine storage initialization status");
                ViewBag.IsStorageInitialized = false;
                Console.WriteLine($"Upload page - Error checking storage status: {ex.Message}");
            }

            return View(new FileUploadModel());
        }

        // POST: Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        // Validate file type
                        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                        var fileExtension = Path.GetExtension(model.ProofOfPayment.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("ProofOfPayment", "Only PDF, JPG, PNG, DOC, and DOCX files are allowed.");
                            return View(model);
                        }

                        // Validate file size (max 10MB)
                        if (model.ProofOfPayment.Length > 10 * 1024 * 1024)
                        {
                            ModelState.AddModelError("ProofOfPayment", "File size must be less than 10MB.");
                            return View(model);
                        }

                        string fileName;

                        if (_useFunctions)
                        {
                            // Functions handles uploading to both shares
                            fileName = await _functionsApi.UploadFileAsync(model.ProofOfPayment, "payment-proofs", "uploads");
                        }
                        else
                        {
                            // Upload to file share for payment proofs
                            fileName = await _storageService.UploadFileAsync(model.ProofOfPayment, "payment-proofs", "uploads");

                            // Also upload to contracts file share for record keeping
                            await _storageService.UploadFileAsync(model.ProofOfPayment, "contracts", "payments");
                        }

                        // Log successful upload
                        _logger.LogInformation("File uploaded successfully: {FileName}", fileName);

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";

                        // Clear the model for a fresh form (following lecturer's approach)
                        return View(new FileUploadModel());
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file");

                    // Provide more specific error messages
                    if (ex.Message.Contains("ShareNotFound"))
                    {
                        ModelState.AddModelError("", "Storage not initialized. Please go to the home page and click 'Initialize Azure Storage' first.");
                    }
                    else if (ex.Message.Contains("connection"))
                    {
                        ModelState.AddModelError("", "Cannot connect to Azure Storage. Please check your connection string.");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                    }
                }
            }
            return View(model);
        }
    }
}