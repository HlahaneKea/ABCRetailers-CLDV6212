using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using System.Net;

namespace ABCRetailers.Functions.Functions
{
    public class ProductsFunctions
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _connectionString;

        public ProductsFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProductsFunctions>();

            _connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection is not configured");

            _tableServiceClient = new TableServiceClient(_connectionString);
            _blobServiceClient = new BlobServiceClient(_connectionString);
        }

        [Function("GetProducts")]
        public async Task<HttpResponseData> GetProducts(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all products");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("products");
                await tableClient.CreateIfNotExistsAsync();

                var products = new List<ProductDto>();
                await foreach (var product in tableClient.QueryAsync<ProductEntity>())
                {
                    products.Add(product.ToDto());
                }

                return await HttpJson.WriteJsonAsync(req, products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("GetProduct")]
        public async Task<HttpResponseData> GetProduct(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Getting product with ID: {id}");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("products");
                var response = await tableClient.GetEntityAsync<ProductEntity>("products", id);

                return await HttpJson.WriteJsonAsync(req, response.Value.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving product {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.NotFound);
            }
        }

        [Function("CreateProduct")]
        public async Task<HttpResponseData> CreateProduct(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Creating new product");

            try
            {
                var (fields, files) = await MultipartHelper.ParseMultipartAsync(req);

                // Extract product data from form fields
                var productDto = new ProductDto
                {
                    ProductName = fields.GetValueOrDefault("ProductName", ""),
                    Description = fields.GetValueOrDefault("Description", ""),
                    Price = double.Parse(fields.GetValueOrDefault("Price", "0")),
                    StockAvailable = int.Parse(fields.GetValueOrDefault("StockAvailable", "0"))
                };

                // Generate unique ID
                string productId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
                productDto.ProductId = productId;
                productDto.RowKey = productId;
                productDto.PartitionKey = "products";

                // Handle image upload to blob storage
                if (files.TryGetValue("imageFile", out var imageStream) && imageStream.Length > 0)
                {
                    var fileName = fields.GetValueOrDefault("imageFileName", "product.jpg");
                    productDto.ImageUrl = await UploadImageToBlobAsync(imageStream, fileName);
                }

                var entity = productDto.ToEntity();

                var tableClient = _tableServiceClient.GetTableClient("products");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(entity);

                return await HttpJson.WriteJsonAsync(req, entity.ToDto(), HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("UpdateProduct")]
        public async Task<HttpResponseData> UpdateProduct(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Updating product with ID: {id}");

            try
            {
                var (fields, files) = await MultipartHelper.ParseMultipartAsync(req);

                // Get existing product
                var tableClient = _tableServiceClient.GetTableClient("products");
                var existingProduct = await tableClient.GetEntityAsync<ProductEntity>("products", id);

                // Update fields
                existingProduct.Value.ProductName = fields.GetValueOrDefault("ProductName", existingProduct.Value.ProductName);
                existingProduct.Value.Description = fields.GetValueOrDefault("Description", existingProduct.Value.Description);

                if (fields.ContainsKey("Price"))
                    existingProduct.Value.Price = double.Parse(fields["Price"]);

                if (fields.ContainsKey("StockAvailable"))
                    existingProduct.Value.StockAvailable = int.Parse(fields["StockAvailable"]);

                // Handle image upload if provided
                if (files.TryGetValue("imageFile", out var imageStream) && imageStream.Length > 0)
                {
                    var fileName = fields.GetValueOrDefault("imageFileName", "product.jpg");
                    existingProduct.Value.ImageUrl = await UploadImageToBlobAsync(imageStream, fileName);
                }

                await tableClient.UpsertEntityAsync(existingProduct.Value, TableUpdateMode.Replace);

                return await HttpJson.WriteJsonAsync(req, existingProduct.Value.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> DeleteProduct(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Deleting product with ID: {id}");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("products");
                await tableClient.DeleteEntityAsync("products", id);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        // Helper method to upload image to blob storage
        private async Task<string> UploadImageToBlobAsync(Stream imageStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("productimages");
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(imageStream, overwrite: true);

            // Generate SAS token for public access
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = "productimages",
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var (accountName, accountKey) = ExtractAccountInfo(_connectionString);
            var credential = new StorageSharedKeyCredential(accountName, accountKey);
            var sasToken = sasBuilder.ToSasQueryParameters(credential);

            return $"{blobClient.Uri}?{sasToken}";
        }

        private (string accountName, string accountKey) ExtractAccountInfo(string connectionString)
        {
            var parts = connectionString.Split(';');
            var accountNamePart = parts.FirstOrDefault(p => p.StartsWith("AccountName="));
            var accountKeyPart = parts.FirstOrDefault(p => p.StartsWith("AccountKey="));

            if (accountNamePart == null || accountKeyPart == null)
            {
                throw new InvalidOperationException("AccountName or AccountKey not found in connection string");
            }

            var accountName = accountNamePart.Substring("AccountName=".Length);
            var accountKey = accountKeyPart.Substring("AccountKey=".Length);

            return (accountName, accountKey);
        }
    }
}

