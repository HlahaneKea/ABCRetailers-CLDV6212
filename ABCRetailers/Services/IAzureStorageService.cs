using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Azure.Data.Tables;

namespace ABCRetailers.Services
{
    public interface IAzureStorageService
    {
        // Table operations
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string customerId);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string customerId);

        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string productId);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(string productId);

        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string orderId);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string orderId);

        // Blob operations
        Task<string> UploadImageAsync(IFormFile imageFile, string containerName);
        Task DeleteImageAsync(string imageUrl, string containerName);

        // Queue operations
        Task SendMessageAsync(string queueName, string message);

        // File Share operations
        Task<string> UploadFileAsync(IFormFile file, string shareName, string directoryName);
        Task DeleteFileAsync(string fileUrl, string shareName, string directoryName);

        // Dashboard operations
        Task<HomeViewModel> GetDashboardDataAsync();

        // Storage initialization
        Task InitializeStorageManuallyAsync();
        Task<bool> IsStorageInitializedAsync();

        // Generic table operations for backward compatibility
        Task<T?> GetEntityAsync<T>(string tableName, string id) where T : class, ITableEntity, new();
        Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();
        Task AddEntityAsync<T>(T entity) where T : class, ITableEntity;
        Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity;
        Task DeleteEntityAsync<T>(string tableName, string id) where T : class, ITableEntity;
    }
}