using System.Text.Json;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Azure;
using Azure.Data.Tables;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Common;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Queues;
using Azure.Storage.Sas;

namespace ABCRetailers.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly string _connectionString;
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;

        public AzureStorageService(IConfiguration configuration)
        {
            try
            {
                _connectionString = configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";
                _tableServiceClient = new TableServiceClient(_connectionString);
                _blobServiceClient = new BlobServiceClient(_connectionString);
                _queueServiceClient = new QueueServiceClient(_connectionString);
                _shareServiceClient = new ShareServiceClient(_connectionString);

                // Initialize Azure Storage resources
                InitializeStorageAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Azure Storage Service: {ex.Message}");
                // Don't throw from constructor, but log the error
                // The service will still be created but may not work properly
            }
        }

        private async Task InitializeStorageAsync()
        {
            try
            {
                Console.WriteLine("Starting Azure Storage initialization...");

                // Create tables
                Console.WriteLine("Creating tables...");
                await CreateTableIfNotExistsAsync("customers");
                await CreateTableIfNotExistsAsync("products");
                await CreateTableIfNotExistsAsync("orders");

                // Create blob containers
                Console.WriteLine("Creating blob containers...");
                await CreateBlobContainerIfNotExistsAsync("productimages");
                await CreateBlobContainerIfNotExistsAsync("paymentproofs");

                // Create queues
                Console.WriteLine("Creating queues...");
                await CreateQueueIfNotExistsAsync("order-notifications");
                await CreateQueueIfNotExistsAsync("stock-updates");

                // Create file shares
                Console.WriteLine("Creating file shares...");
                await CreateFileShareIfNotExistsAsync("contracts");
                await CreateFileShareIfNotExistsAsync("payment-proofs");

                Console.WriteLine("Azure Storage initialization completed successfully!");

                // Verify the creation by checking if they exist
                await VerifyStorageInitializationAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during Azure Storage initialization: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                throw; // Re-throw to ensure errors are not silently ignored
            }
        }

        private async Task CreateTableIfNotExistsAsync(string tableName)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - table creation failures shouldn't break the main operation
                Console.WriteLine($"Warning: Failed to create table '{tableName}': {ex.Message}");
            }
        }

        private async Task CreateBlobContainerIfNotExistsAsync(string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - blob container creation failures shouldn't break the main operation
                Console.WriteLine($"Warning: Failed to create blob container '{containerName}': {ex.Message}");
            }
        }

        private async Task CreateQueueIfNotExistsAsync(string queueName)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - queue creation failures shouldn't break the main operation
                Console.WriteLine($"Warning: Failed to create queue '{queueName}': {ex.Message}");
            }
        }

        private async Task CreateFileShareIfNotExistsAsync(string shareName)
        {
            try
            {
                Console.WriteLine($"Attempting to create file share: {shareName}");
                var shareClient = _shareServiceClient.GetShareClient(shareName);

                // Check if share exists first
                var shareExists = await shareClient.ExistsAsync();
                if (!shareExists.Value)
                {
                    Console.WriteLine($"File share '{shareName}' does not exist, creating...");
                    await shareClient.CreateAsync();
                    Console.WriteLine($"Successfully created file share: {shareName}");
                }
                else
                {
                    Console.WriteLine($"File share '{shareName}' already exists");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file share '{shareName}': {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                throw; // Re-throw to ensure errors are not silently ignored during initialization
            }
        }

        // Customer operations
        public async Task<List<Customer>> GetCustomersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            var customers = new List<Customer>();

            await foreach (var customer in tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }

        public async Task<Customer?> GetCustomerAsync(string customerId)
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            var customer = await tableClient.GetEntityAsync<Customer>("customers", customerId);
            return customer.Value;
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            // Generate a unique ID using timestamp + GUID to minimize conflicts
            string customerId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

            customer.CustomerId = customerId;
            customer.PartitionKey = "customers";
            customer.RowKey = customerId;

            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.AddEntityAsync(customer);
            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            // Ensure CustomerId is preserved from RowKey if it's empty
            if (string.IsNullOrEmpty(customer.CustomerId) && !string.IsNullOrEmpty(customer.RowKey))
            {
                customer.CustomerId = customer.RowKey;
            }

            customer.PartitionKey = "customers";
            customer.RowKey = customer.CustomerId;

            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
            return customer;
        }

        public async Task DeleteCustomerAsync(string customerId)
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.DeleteEntityAsync("customers", customerId);
        }

        // Product operations
        public async Task<List<Product>> GetProductsAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            var products = new List<Product>();

            await foreach (var product in tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }

            return products;
        }

        public async Task<Product?> GetProductAsync(string productId)
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            var product = await tableClient.GetEntityAsync<Product>("products", productId);
            return product.Value;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            // Generate a unique ID using timestamp + GUID to minimize conflicts
            string productId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

            product.ProductId = productId;
            product.PartitionKey = "products";
            product.RowKey = productId;

            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.AddEntityAsync(product);
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            product.PartitionKey = "products";
            product.RowKey = product.ProductId;

            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
            return product;
        }

        public async Task DeleteProductAsync(string productId)
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.DeleteEntityAsync("products", productId);
        }

        // Order operations
        public async Task<List<Order>> GetOrdersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            var orders = new List<Order>();

            await foreach (var order in tableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }

            return orders;
        }

        public async Task<Order?> GetOrderAsync(string orderId)
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            var order = await tableClient.GetEntityAsync<Order>("orders", orderId);
            return order.Value;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Generate a unique ID using timestamp + GUID to minimize conflicts
            string orderId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

            order.RowKey = orderId;
            order.PartitionKey = "orders";

            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.AddEntityAsync(order);
            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            order.PartitionKey = "orders";
            order.RowKey = order.OrderId;

            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
            return order;
        }

        public async Task DeleteOrderAsync(string orderId)
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.DeleteEntityAsync("orders", orderId);
        }

        // Blob operations
        public async Task<string> UploadImageAsync(IFormFile imageFile, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobName = $"{Guid.NewGuid()}_{imageFile.FileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = imageFile.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            // Generate a SAS token for public access (valid for 1 year)
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b", // b for blob
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Extract account info from connection string and create credential
            var (accountName, accountKey) = ExtractAccountInfoFromConnectionString(_connectionString);
            var credential = new StorageSharedKeyCredential(accountName, accountKey);
            var sasToken = sasBuilder.ToSasQueryParameters(credential);
            return $"{blobClient.Uri}?{sasToken}";
        }

        private (string accountName, string accountKey) ExtractAccountInfoFromConnectionString(string connectionString)
        {
            // Parse connection string to extract AccountName and AccountKey
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

        public async Task DeleteImageAsync(string imageUrl, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }

        // Queue operations
        public async Task SendMessageAsync(string queueName, string message)
        {
            try
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueName);
                await queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message)));
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - queue failures shouldn't break the main operation
                // In a production environment, you might want to log this to a proper logging service
                Console.WriteLine($"Warning: Failed to send message to queue '{queueName}': {ex.Message}");
            }
        }

        // File Share operations
        public async Task<string> UploadFileAsync(IFormFile file, string shareName, string directoryName)
        {
            try
            {
                Console.WriteLine($"Attempting to upload file to share: {shareName}, directory: {directoryName}");
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                Console.WriteLine($"Share client created for: {shareName}");

                // Check if share exists
                var shareExists = await shareClient.ExistsAsync();
                if (!shareExists.Value)
                {
                    throw new InvalidOperationException($"File share '{shareName}' does not exist. Please initialize Azure Storage first.");
                }

                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                Console.WriteLine($"Directory client created for: {directoryName}");

                await directoryClient.CreateIfNotExistsAsync();
                Console.WriteLine($"Directory created/verified: {directoryName}");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var fileClient = directoryClient.GetFileClient(fileName);
                Console.WriteLine($"File client created for: {fileName}");

                using var stream = file.OpenReadStream();
                await fileClient.CreateAsync(file.Length);
                Console.WriteLine($"File created with length: {file.Length}");

                await fileClient.UploadRangeAsync(new Azure.HttpRange(0, file.Length), stream);
                Console.WriteLine($"File content uploaded successfully");

                return fileClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to share '{shareName}' directory '{directoryName}': {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                throw; // Re-throw to provide proper error information
            }
        }



        public async Task DeleteFileAsync(string fileUrl, string shareName, string directoryName)
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - file deletion failures shouldn't break the main operation
                Console.WriteLine($"Warning: Failed to delete file from share '{shareName}' directory '{directoryName}': {ex.Message}");
            }
        }

        // Dashboard operations
        public async Task<HomeViewModel> GetDashboardDataAsync()
        {
            var customers = await GetCustomersAsync();
            var products = await GetProductsAsync();
            var orders = await GetOrdersAsync();

            var featuredProducts = products.Take(5).ToList();

            return new HomeViewModel
            {
                FeaturedProducts = featuredProducts,
                CustomerCount = customers.Count,
                ProductCount = products.Count,
                OrderCount = orders.Count
            };
        }

        // Manual storage initialization method
        public async Task InitializeStorageManuallyAsync()
        {
            await InitializeStorageAsync();
        }

        // Verify storage initialization
        private async Task VerifyStorageInitializationAsync()
        {
            try
            {
                Console.WriteLine("Verifying storage initialization...");
                var isInitialized = await IsStorageInitializedAsync();
                Console.WriteLine($"Storage verification result: {isInitialized}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during storage verification: {ex.Message}");
            }
        }

        // Check if storage is initialized
        public async Task<bool> IsStorageInitializedAsync()
        {
            try
            {
                Console.WriteLine("=== Starting storage initialization check ===");

                // Check if the service clients are properly initialized
                if (_tableServiceClient == null || _blobServiceClient == null || _queueServiceClient == null || _shareServiceClient == null)
                {
                    Console.WriteLine("One or more service clients are null");
                    return false;
                }

                Console.WriteLine("All service clients are available, checking resources...");

                // Check if file shares exist (this is what we actually need for file uploads)
                var contractsShareClient = _shareServiceClient.GetShareClient("contracts");
                var paymentProofsShareClient = _shareServiceClient.GetShareClient("payment-proofs");

                var contractsExists = await contractsShareClient.ExistsAsync();
                var paymentProofsExists = await paymentProofsShareClient.ExistsAsync();

                Console.WriteLine($"File shares exist - Contracts: {contractsExists.Value}, Payment-proofs: {paymentProofsExists.Value}");

                // If file shares exist, storage is considered initialized (since that's what we're checking for)
                var result = contractsExists.Value && paymentProofsExists.Value;
                Console.WriteLine($"Final result: {result}");
                Console.WriteLine("=== Storage initialization check completed ===");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking storage initialization: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                return false;
            }
        }

        // Generic table operations for backward compatibility
        public async Task<T?> GetEntityAsync<T>(string tableName, string id) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            try
            {
                var entity = await tableClient.GetEntityAsync<T>(tableName, id);
                // Check if entity exists before accessing .Value
                if (entity == null)
                {
                    return null;
                }

                // Special handling for Customer entities to ensure CustomerId is populated
                if (entity.Value is Customer customer)
                {
                    if (string.IsNullOrEmpty(customer.CustomerId) && !string.IsNullOrEmpty(customer.RowKey))
                    {
                        customer.CustomerId = customer.RowKey;
                    }
                }

                return entity.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            var entities = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task AddEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.AddEntityAsync(entity);
        }

        public async Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync<T>(string tableName, string id) where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.DeleteEntityAsync(tableName, id);
        }

        private string GetTableName<T>()
        {
            return typeof(T).Name.ToLower() + "s";
        }
    }
}