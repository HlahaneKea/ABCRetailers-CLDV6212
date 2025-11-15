using ABCRetailers.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ABCRetailers.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionsApiClient> _logger;
        private readonly string _functionKey;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public FunctionsApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<FunctionsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["Functions:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:7071/api/";
            }

            _functionKey = configuration["Functions:FunctionKey"] ?? "";

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _functionKey);
        }

        #region Customer Operations

        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("customers");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Customer>>(content, JsonOptions) ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetCustomers function");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerAsync(string customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"customers/{customerId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Customer>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling GetCustomer function for {customerId}");
                return null;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("customers", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Customer>(responseContent, JsonOptions) ?? customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CreateCustomer function");
                throw;
            }
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var customerId = customer.RowKey ?? customer.CustomerId;
                var response = await _httpClient.PutAsync($"customers/{customerId}", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Customer>(responseContent, JsonOptions) ?? customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UpdateCustomer function");
                throw;
            }
        }

        public async Task DeleteCustomerAsync(string customerId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"customers/{customerId}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling DeleteCustomer function for {customerId}");
                throw;
            }
        }

        #endregion

        #region Product Operations

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("products");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Product>>(content, JsonOptions) ?? new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetProducts function");
                throw;
            }
        }

        public async Task<Product?> GetProductAsync(string productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"products/{productId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling GetProduct function for {productId}");
                return null;
            }
        }

        public async Task<Product> CreateProductAsync(Product product, IFormFile? imageFile)
        {
            try
            {
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(product.ProductName), "ProductName");
                formData.Add(new StringContent(product.Description), "Description");
                formData.Add(new StringContent(product.Price.ToString()), "Price");
                formData.Add(new StringContent(product.StockAvailable.ToString()), "StockAvailable");

                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageContent = new StreamContent(imageFile.OpenReadStream());
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(imageContent, "imageFile", imageFile.FileName);
                    formData.Add(new StringContent(imageFile.FileName), "imageFileName");
                }

                var response = await _httpClient.PostAsync("products", formData);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(responseContent, JsonOptions) ?? product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CreateProduct function");
                throw;
            }
        }

        public async Task<Product> UpdateProductAsync(Product product, IFormFile? imageFile)
        {
            try
            {
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(product.ProductName), "ProductName");
                formData.Add(new StringContent(product.Description), "Description");
                formData.Add(new StringContent(product.Price.ToString()), "Price");
                formData.Add(new StringContent(product.StockAvailable.ToString()), "StockAvailable");

                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageContent = new StreamContent(imageFile.OpenReadStream());
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(imageContent, "imageFile", imageFile.FileName);
                    formData.Add(new StringContent(imageFile.FileName), "imageFileName");
                }

                var productId = product.RowKey ?? product.ProductId;
                var response = await _httpClient.PutAsync($"products/{productId}", formData);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(responseContent, JsonOptions) ?? product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UpdateProduct function");
                throw;
            }
        }

        public async Task DeleteProductAsync(string productId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"products/{productId}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling DeleteProduct function for {productId}");
                throw;
            }
        }

        #endregion

        #region Order Operations

        public async Task<List<Order>> GetOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("orders");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Order>>(content, JsonOptions) ?? new List<Order>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetOrders function");
                throw;
            }
        }

        public async Task<Order?> GetOrderAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"orders/{orderId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling GetOrder function for {orderId}");
                return null;
            }
        }

        // This sends order to queue (not directly to table)
        public async Task CreateOrderAsync(Order order)
        {
            try
            {
                var orderMessage = new
                {
                    customerId = order.CustomerId,
                    username = order.Username,
                    productId = order.ProductId,
                    productName = order.ProductName,
                    orderDate = order.OrderDate,
                    quantity = order.Quantity,
                    unitPrice = order.UnitPrice,
                    totalPrice = order.TotalPrice,
                    status = order.Status
                };

                var json = JsonSerializer.Serialize(orderMessage, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("orders", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Order submitted to queue successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CreateOrder function");
                throw;
            }
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            try
            {
                var json = JsonSerializer.Serialize(order, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var orderId = order.RowKey ?? order.OrderId;
                var response = await _httpClient.PutAsync($"orders/{orderId}", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(responseContent, JsonOptions) ?? order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UpdateOrder function");
                throw;
            }
        }

        public async Task DeleteOrderAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"orders/{orderId}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling DeleteOrder function for {orderId}");
                throw;
            }
        }

        #endregion

        #region File Operations

        public async Task<string> UploadFileAsync(IFormFile file, string shareName, string directoryName)
        {
            try
            {
                using var formData = new MultipartFormDataContent();

                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                formData.Add(fileContent, "file", file.FileName);
                formData.Add(new StringContent(shareName), "shareName");
                formData.Add(new StringContent(directoryName), "directoryName");
                formData.Add(new StringContent(file.FileName), "fileName");

                var response = await _httpClient.PostAsync("files/upload-multiple", formData);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent, JsonOptions);

                // Return the paymentProofsUrl
                return result.GetProperty("paymentProofsUrl").GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UploadFile function");
                throw;
            }
        }

        #endregion
    }
}

