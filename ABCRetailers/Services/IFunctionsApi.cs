using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface IFunctionsApi
    {
        // Customer operations
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string customerId);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string customerId);

        // Product operations
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string productId);
        Task<Product> CreateProductAsync(Product product, IFormFile? imageFile);
        Task<Product> UpdateProductAsync(Product product, IFormFile? imageFile);
        Task DeleteProductAsync(string productId);

        // Order operations
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string orderId);
        Task CreateOrderAsync(Order order); // Sends to queue
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string orderId);

        // File operations
        Task<string> UploadFileAsync(IFormFile file, string shareName, string directoryName);
    }
}

